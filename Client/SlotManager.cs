using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Client.Services;
using Common.Services;
using Common.Slots;
using Common.Beans;
using Common.Util;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Remoting;

namespace Client
{

    public interface ISlotManager
    {
        bool StartReservation(ReservationRequest req);

        List<CalendarSlot> ReadCalendar();

        void Connect();

        void Disconnect();
    }

    // A delegate type for hooking up change notifications.
    public delegate void ConnectedEventHandler(string userID, IBookingService stub);

    // A delegate type for hooking up change notifications.
    public delegate void DisconnectedEventHandler(int resID, string userID);

    public delegate void InitiatorDelegate(int resID, int slotID, string userID, bool ack);

    public class SlotManager : MarshalByRefObject, IBookingService, ISlotManager
    {
        public delegate void AbortDelegate(int resID);

        private Dictionary<int, CalendarSlot> _calendar;
        private Dictionary<int, Reservation> _activeReservations;
        private Dictionary<int, Reservation> _committedReservations;

        private string _userName;
        private int _port;

        private List<ServerMetadata> _servers;

        private IClientMonitor _clientMonitor;
        private IMessageDispatcher _msgDispatcher;

        private Thread _monitorThread;

        private DisconnectedEventHandler _disconnectInterested;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public SlotManager(string userName, int port, List<ServerMetadata> servers)
        {
            _calendar = new Dictionary<int, CalendarSlot>();
            _activeReservations = new Dictionary<int, Reservation>();
            _committedReservations = new Dictionary<int, Reservation>();
            _userName = userName;
            _port = port;
            _servers = servers;
            _msgDispatcher = new MessageDispatcher(_userName);
            _clientMonitor = new ClientMonitor(_msgDispatcher, _userName, _servers);
            _disconnectInterested = new DisconnectedEventHandler(_msgDispatcher.ClientDisconnected) + new DisconnectedEventHandler(_clientMonitor.Disconnected);
            _monitorThread = null;
        }

        /*
         * SLOT MANAGER METHODS
         */

        public bool StartReservation(ReservationRequest req)
        {
            //Updates request with sequence number
            int resID = RetrieveSequenceNumber();

            //Create and populate local reservation
            Reservation res = CreateReservation(req, resID, _userName, Helper.GetIPAddress(), _port);
            //Mark slots initial states
            List<ReservationSlot> reservationSlots = CreateReservationSlots(req, resID);

            //Update reservation request, removing aborted slots
            foreach (ReservationSlot slot in new List<ReservationSlot>(reservationSlots))
            {
                if (slot.State == ReservationSlotState.ABORTED)
                {
                    Log.Show(_userName, "Slot " + slot + " not available on initiator. Removing from reservation.");
                    //removing slot of original request, since it will be passed to participants
                    reservationSlots.Remove(slot);
                    req.Slots.Remove(slot.SlotID);
                }
            }
            res.Slots = reservationSlots;

            Log.Show(_userName, "Starting reservation " + res.ReservationID + ". With participants " + string.Join(",", res.Participants) + ". Slots: " + string.Join(",", res.Slots));

            //If no slots are available, cancel reservation
            if (res.Slots.Count == 0)
            {
                Log.Show(_userName, "No available slots on initiator, aborting reservation.");
                return false;
            }

            //just the initiator is on the reservation
            if (req.Users.Count == 1)
            {
                foreach (ReservationSlot slot in res.Slots)
                {
                    //GET LOCK HERE
                    CalendarSlot cSlot = _calendar[slot.SlotID];
                    if (slot.State != ReservationSlotState.ABORTED && !cSlot.Locked && cSlot.State != CalendarSlotState.ASSIGNED)
                    {
                        AssignCalendarSlot(res, slot, true);
                        //RELEASE LOCK HERE
                        return true;
                    }
                    else if (slot.State != ReservationSlotState.ABORTED)
                    {
                        AbortReservationSlot(slot, true);
                    }
                }

                //RELEASE LOCK HERE (finally)

                //TODO check this
                return false;
            }

            _clientMonitor.MonitorReservation(res);

            //Add reservation to map of active reservations
            _activeReservations[res.ReservationID] = res;

            foreach (string participantID in res.Participants)
            {
                if (!participantID.Equals(_userName))
                {
                    _msgDispatcher.SendMessage(MessageType.INIT_RESERVATION, resID, participantID, req, _userName, Helper.GetIPAddress(), _port);
                }
            }

            return true;
        }

        public List<CalendarSlot> ReadCalendar()
        {
            return _calendar.Values.ToList();
        }

        public void Connect()
        {
            _monitorThread = new Thread(new ThreadStart(_clientMonitor.StartMonitoring));
            _monitorThread.Start();
        }


        public void Disconnect()
        {
            _monitorThread.Abort();
            _monitorThread = null;
            foreach (Reservation res in new List<Reservation>(_activeReservations.Values))
            {
               res.InitiatorStub.Disconnected(res.ReservationID, _userName);
            }
        }

        /*
         * BOOKING SERVICE INITIATOR
         */

        void IBookingService.InitReservationReply(List<ReservationSlot> slots, string userID)
        {
            Log.Show(_userName, "Received init reservation reply from participant " + userID + ". Slots: " + string.Join(", ", slots));
            Reservation res = UpdateReservation(slots, userID);

            //Check if all participants replied before moving to next phase
            if (res != null && res.Replied.Count == (res.Participants.Count - 1))
            {
                Log.Show(_userName, "Reservation " + res.ReservationID + " was initialized by all participants.");

                res.Replied.Clear();

                BookNextSlot(res);
            }
        }

        void IBookingService.BookReply(int resID, int slotID, string userID, bool ack)
        {
            Log.Show(_userName, "Received " + (ack ? "POSITIVE" : "NEGATIVE") + " book ACK from participant " + userID + " for slot " + slotID + " of  reservation " + resID);

            Reservation res;

            //Check if all participants replied before moving to next phase
            if (CollectReply(resID, slotID, userID, ack, out res))
            {
                Log.Show(_userName, "All participants replied for booking of slot " + slotID + " of reservation " + res.ReservationID);

                PrepareCommitOrBookNextSlot(res, slotID);
            }
        }

        void IBookingService.PrepareCommitReply(int resID, int slotID, string userID, bool ack)
        {

            Log.Show(_userName, "Received " + (ack ? "POSITIVE" : "NEGATIVE") + " pre commit ACK from participant " + userID + " for slot " + slotID + " of  reservation " + resID);

            Reservation res;

            //Check if all participants replied before moving to next phase
            if (CollectReply(resID, slotID, userID, ack, out res))
            {
                Log.Show(_userName, "All participants replied for pre-commit of slot " + slotID + " of reservation " + res.ReservationID);

                CommitSlot(res, slotID);
            }
        }

        void IBookingService.DoCommitReply(int resID, int slotID, string userID, bool ack)
        {

            Log.Show(_userName, "Received commit ACK from participant " + userID + " for slot " + slotID + " of  reservation " + resID);

            Reservation res;

            //Check if all participants replied before moving to next phase
            if (CollectReply(resID, slotID, userID, ack, out res) && res.Replied.Count == (res.Participants.Count - 1))
            {
                Log.Show(_userName, "All participants committed slot " + slotID + " of reservation " + res.ReservationID + ". Cleaning up reservation");

                CleanReservation(res);
                _clientMonitor.RemoveReservation(resID);
            }
        }

        /*
         * BOOKING SERVICE PATICIPANT
         */

        void IBookingService.InitReservation(ReservationRequest req, int resID, string initiatorID, string initiatorIP, int initiatorPort)
        {
            //Create and populate local reservation
            Reservation res = CreateReservation(req, resID, _userName, initiatorIP, initiatorPort);

            //Mark slots initial states
            res.Slots = CreateReservationSlots(req, resID);

            Log.Show(_userName, "Initializing reservation " + res.ReservationID + ". Initiator: " + res.InitiatorID + ". Slots: " + string.Join(", ", res.Slots));

            bool abortedAll = true;
            foreach (ReservationSlot slot in res.Slots)
            {
                if (slot.State != ReservationSlotState.ABORTED)
                {
                    abortedAll = false;
                    break;
                }
            }

            //If no slots are available, don't store reservation
            if (abortedAll)
            {
                Log.Show(_userName, "No available slots on this participant. Reservation will not be persisted.");
            }
            else
            {
                //Add reservation to map of active reservations
                _activeReservations[res.ReservationID] = res;
            }

            //Reply to initiator
            String connectionString = "tcp://" + initiatorIP + ":" + initiatorPort + "/" + initiatorID + "/" + Common.Constants.BOOKING_SERVICE_NAME;

            try
            {
                IBookingService initiator = (IBookingService)Activator.GetObject(
                                                                        typeof(ILookupService),
                                                                        connectionString);

                res.InitiatorStub = initiator;
                initiator.InitReservationReply(res.Slots, _userName);
            }
            catch (SocketException)
            {
                Log.Show(_userName, "ERROR: Initiator is not online.");
                //TODO: do something...
            }

        }

        void IBookingService.BookSlot(int resID, int slotID)
        {
            Log.Show(_userName, "Received book request from initiator for slot " + slotID + " of  reservation " + resID);

            Reservation res;
            if (!_activeReservations.TryGetValue(resID, out res))
            {
                Log.Show(_userName, "WARN: Received book request from unknown reservation " + resID);
                return;
            }

            ReservationSlot resSlot = GetSlotAndAbortPredecessors(res, slotID);

            if (resSlot == null)
            {
                Log.Show(_userName, "WARN: Received book request from unknown reservation " + resID);
                return;
            }

            //GET LOCK HERE

            CalendarSlot calendarSlot = _calendar[slotID];
            calendarSlot.WaitingBook.Remove(slotID);

            //if slot is already booked and resID is bigger than ID holding lock, add this request to queue
            if (calendarSlot.State == CalendarSlotState.BOOKED && resID > calendarSlot.ReservationID)
            {
                Log.Show(_userName, "Book request for slot " + slotID + " of  reservation " + resID + " was enqueued. Higher priority request " + calendarSlot.ReservationID + " already booked.");
                calendarSlot.BookQueue.Add(resID);
                return;
            }

            bool ack = !calendarSlot.Locked && calendarSlot.State != CalendarSlotState.ASSIGNED;

            if (ack)
            {
                BookCalendarSlot(res, resSlot);
            }
            else
            {
                AbortReservationSlot(resSlot, true);
            }

            //RELEASE LOCK HERE

            res.InitiatorStub.BookReply(resSlot.ReservationID, resSlot.SlotID, _userName, ack);
        }

        void IBookingService.PrepareCommit(int resID, int slotID)
        {
            Log.Show(_userName, "Received prepare commit request from initiator for slot " + slotID + " of  reservation " + resID);

            //Fetch reservation and slot objects

            Reservation res;
            if (!_activeReservations.TryGetValue(resID, out res))
            {
                Log.Show(_userName, "WARN: Received pre-commit request from unknown reservation " + resID);
                return;
            }

            ReservationSlot resSlot = GetSlot(res, slotID);

            if (resSlot == null)
            {
                Log.Show(_userName, "WARN: Received prepare commit request from unknown reservation " + resID);
                return;
            }

            //GET LOCK HERE

            CalendarSlot calendarSlot = _calendar[slotID];

            bool ack = !calendarSlot.Locked && calendarSlot.State == CalendarSlotState.BOOKED && calendarSlot.ReservationID == resID;

            if (ack)
            {
                LockCalendarSlot(res, resSlot);
            }
            else
            {
                AbortReservationSlot(resSlot, true);
            }

            //RELEASE LOCK HERE

            res.InitiatorStub.PrepareCommitReply(resSlot.ReservationID, resSlot.SlotID, _userName, ack);
        }

        void IBookingService.DoCommit(int resID, int slotID)
        {
            Log.Show(_userName, "Received Do Commit from initiator for slot " + slotID + " of  reservation " + resID + ". Assigning calendar.");

            //Fetch reservation and slot objects

            Reservation res;
            if (!_activeReservations.TryGetValue(resID, out res))
            {
                Log.Show(_userName, "WARN: Received doCommit from unknown reservation " + resID);
                return;
            }

            ReservationSlot resSlot = GetSlot(res, slotID);

            if (resSlot == null)
            {
                Log.Show(_userName, "WARN: Received do commit request from unknown reservation " + resID);
                return;
            }

            //GET LOCK HERE

            AssignCalendarSlot(res, resSlot, true);

            //RELEASE LOCK HERE

            res.InitiatorStub.DoCommitReply(resSlot.ReservationID, resSlot.SlotID, _userName, true);
        }

        void IBookingService.Disconnected(int resId, string userID)
        {
            Log.Show(_userName, "Participant " + userID + " from reservartion " + resId + " disconnected. Notifying client monitor.");
            _disconnectInterested.Invoke(resId, userID);
        }

        void IBookingService.AbortReservation(int resID)
        {
            //TODO VERIFY RACE CONDITIONS ON _activeReservations. MONITORS MAY BE NEEDED

            Log.Show(_userName, "Received Abort Reservation for reservation " + resID + ". Aborting all slots.");

            Reservation res;

            bool committed = false;

            if (_committedReservations.TryGetValue(resID, out res))
            {
                committed = true;
                //TODO: log on puppet master
                Log.Show(_userName, "WARN: OOPS! Received abort for already committed reservation: " + resID + ". Rolling back.");
            }
            else if (!_activeReservations.TryGetValue(resID, out res))
            {
                Log.Show(_userName, "Received abort for unknown reservation " + resID);
                return;
            }

            foreach (ReservationSlot slot in res.Slots)
            {
                if (slot.State != ReservationSlotState.ABORTED)
                {
                    AbortReservationSlot(slot, false);
                }
            }

            if (committed)
            {
                _committedReservations.Remove(resID);
            }
            else
            {
                _activeReservations.Remove(resID);
            }

        }


        /*
         * AUX METHODS
         */

        private void BookNextSlot(Reservation res)
        {
            bool booked = false;

            //LOCK HERE

            foreach (ReservationSlot slot in res.Slots)
            {
                CalendarSlot cSlot = _calendar[slot.SlotID];
                if (slot.State != ReservationSlotState.ABORTED && !cSlot.Locked && cSlot.State != CalendarSlotState.ASSIGNED)
                {
                    booked = true;

                    BookCalendarSlot(res, slot);

                    //RELEASE LOCK HERE

                    Log.Show(_userName, "Starting book process of slot " + slot.SlotID + " from reservation " + res.ReservationID);

                    foreach (string participantID in res.Participants)
                    {
                        if (!participantID.Equals(_userName))
                        {
                            _msgDispatcher.SendMessage(MessageType.BOOK_SLOT, res.ReservationID, participantID, slot.SlotID);
                        }
                    }

                    break;
                }
            }

            if (!booked)
            {
                //RELEASE LOCK HERE

                Log.Show(_userName, "No available slots. Aborting reservation " + res.ReservationID);

                foreach (IBookingService client in res.ClientStubs.Values)
                {
                    try
                    {
                        Log.Show(_userName, "Sending abort to client...");
                        AbortDelegate bookSlot = new AbortDelegate(client.AbortReservation);
                        IAsyncResult RemAr = bookSlot.BeginInvoke(res.ReservationID, null, null);
                    }
                    catch (SocketException e)
                    {
                        Log.Show(_userName, "ERROR: Could not connect to client. Exception: " + e);
                        //TODO: do something
                    }
                }

                //TODO abort reservation on initiator
            }
        }

        private void PrepareCommitOrBookNextSlot(Reservation res, int slotID)
        {
            //GET LOCK HERE

            CalendarSlot calendarSlot = _calendar[slotID];

            if (calendarSlot.Locked || calendarSlot.State == CalendarSlotState.ASSIGNED || calendarSlot.ReservationID != res.ReservationID)
            {
                Log.Show(_userName, "Booking of slot " + slotID + " of reservation " + res.ReservationID + " failed. Trying to book next slot.");
                //RELEASE LOCK HERE

                BookNextSlot(res);
            }
            else
            {
                Log.Show(_userName, "Slot " + slotID + " of reservation " + res.ReservationID + " was booked successfully. Starting commit process.");

                ReservationSlot slot = GetSlot(res, slotID);
                LockCalendarSlot(res, slot);

                //RELEASE LOCK HERE

                foreach (string participantID in res.Participants)
                {
                    if (!participantID.Equals(_userName))
                    {
                        _msgDispatcher.SendMessage(MessageType.PRE_COMMIT, res.ReservationID, participantID, slot.SlotID);
                    }
                }
            }
        }

        private void CommitSlot(Reservation res, int slotID)
        {
            ReservationSlot slot = GetSlot(res, slotID);

            Log.Show(_userName, "Slot " + slotID + " of reservation " + res.ReservationID + " was pre-committed successfully. Assigning calendar slot.");

            AssignCalendarSlot(res, slot, false);

            //RELEASE LOCK HERE

            foreach (string participantID in res.Participants)
            {
                if (!participantID.Equals(_userName))
                {
                    _msgDispatcher.SendMessage(MessageType.DO_COMMIT, res.ReservationID, participantID, slot.SlotID);
                }
            }

            //TODO: Log to puppet master
            Log.Show(_userName, "Reservation " + res.ReservationID + " succesfully assigned to slot " + slotID + " on clients: " + string.Join(",", res.Participants));
        }

        private bool CollectReply(int resID, int slotID, string userID, bool ack, out Reservation res)
        {
            if (!_activeReservations.TryGetValue(resID, out res))
            {
                Log.Show(_userName, "WARN: Received reply from unknown reservation " + resID);
                return false;
            }

            if (res.CurrentSlot == slotID)
            {
                if (ack)
                {
                    res.Replied.Add(userID);
                    if (res.Replied.Count == (res.Participants.Count - 1))
                    {
                        res.Replied.Clear();
                        return true;
                    }
                }
                else
                {
                    _msgDispatcher.ClearMessages(res.Participants, resID);
                    ReservationSlot slot = GetSlot(res, slotID);
                    AbortReservationSlot(slot, false);
                    res.Replied.Clear();
                    BookNextSlot(res);
                }
                return false;
            }

            return false;
        }

        private void AbortReservationSlot(ReservationSlot slot, bool locked)
        {
            Log.Debug(_userName, "Aborting slot " + slot.SlotID + " from reservation " + slot.ReservationID);

            if (slot != null)
            {
                slot.State = ReservationSlotState.ABORTED;
            }

            //IF NOT LOCKED GET LOCK HERE

            FreeCalendarSlot(slot);

            //IF NOT LOCKED RELEASE LOCK HERE
        }

        private void FreeCalendarSlot(ReservationSlot slot)
        {
            CalendarSlot cSlot = _calendar[slot.SlotID];

            cSlot.WaitingBook.Remove(slot.ReservationID);
            cSlot.BookQueue.Remove(slot.ReservationID);

            if (cSlot.State == CalendarSlotState.ACKNOWLEDGED && cSlot.WaitingBook.Count == 0)
            {
                cSlot.State = CalendarSlotState.FREE;
                //_calendar.Remove(slot.SlotID); - Could remove, but will influence on BOOKNEXTSLOT, so setting it FREE for now
            }
            else if (cSlot.State == CalendarSlotState.BOOKED && cSlot.ReservationID == slot.ReservationID)
            {
                cSlot.Locked = false;

                //Send ACK to next reservation on book queue
                if (cSlot.BookQueue.Count > 0)
                {
                    int lowestResID = -1;
                    foreach (int otherResID in cSlot.BookQueue)
                    {
                        if (lowestResID == -1 || lowestResID > otherResID)
                        {
                            lowestResID = otherResID;
                        }
                    }

                    Reservation otherRes;
                    if (_activeReservations.TryGetValue(lowestResID, out otherRes))
                    {
                        InitiatorDelegate bookReply = new InitiatorDelegate(otherRes.InitiatorStub.BookReply);
                        IAsyncResult RemAr = bookReply.BeginInvoke(otherRes.ReservationID, slot.SlotID, _userName, true, null, null);
                        cSlot.BookQueue.Remove(lowestResID);
                    }
                }
            }
        }

        /*
         * CALENDAR SHOULD BE LOCKED BEFORE CALLING THIS METHOD
         */
        private void BookCalendarSlot(Reservation res, ReservationSlot resSlot)
        {
            CalendarSlot calendarSlot = _calendar[resSlot.SlotID];
            calendarSlot.WaitingBook.Remove(resSlot.SlotID);
            res.CurrentSlot = resSlot.SlotID;
            calendarSlot.State = CalendarSlotState.BOOKED;
            calendarSlot.ReservationID = resSlot.ReservationID;
            resSlot.State = ReservationSlotState.TENTATIVELY_BOOKED;
        }

        /*
         * CALENDAR SHOULD BE LOCKED BEFORE CALLING THIS METHOD
         */
        private void LockCalendarSlot(Reservation res, ReservationSlot resSlot)
        {
            //Change calendar and reservation slot states
            CalendarSlot calendarSlot = _calendar[resSlot.SlotID];
            calendarSlot.Locked = true;
            resSlot.State = ReservationSlotState.COMMITTED;
        }

        /*
         * CALENDAR SHOULD BE LOCKED BEFORE CALLING THIS METHOD
         */
        private void AssignCalendarSlot(Reservation res, ReservationSlot resSlot, bool clean)
        {

            //Change calendar and reservation slot states
            CalendarSlot calendarSlot = _calendar[resSlot.SlotID];
            calendarSlot.State = CalendarSlotState.ASSIGNED;
            calendarSlot.ReservationID = res.ReservationID;

            ////SEND ASYNCHRONOUS NACK TO ALL PENDING RESERVATIONS ON THE QUEUE
            foreach (int resID in new List<int>(calendarSlot.BookQueue))
            {
                Reservation otherRes;
                if (_activeReservations.TryGetValue(resID, out otherRes))
                {
                    InitiatorDelegate bookReply = new InitiatorDelegate(otherRes.InitiatorStub.BookReply);
                    IAsyncResult RemAr = bookReply.BeginInvoke(otherRes.ReservationID, resSlot.SlotID, _userName, false, null, null);
                    calendarSlot.BookQueue.Remove(resID);
                }
            }

            foreach (ReservationSlot slot in res.Slots)
            {
                if (slot != resSlot && slot.State != ReservationSlotState.ABORTED)
                {
                    AbortReservationSlot(slot, true);
                }
            }

            if (clean)
            {
                CleanReservation(res);
            }
        }

        private void CleanReservation(Reservation res)
        {
            //Remove from list of active reservations
            _activeReservations.Remove(res.ReservationID);
            _committedReservations[res.ReservationID] = res;
        }

        //Verify calendar slots and create reservation states
        private List<ReservationSlot> CreateReservationSlots(ReservationRequest req, int resID)
        {
            List<ReservationSlot> reservationSlots = new List<ReservationSlot>();

            //LOCK HERE

            foreach (int slot in req.Slots)
            {

                ReservationSlotState state = ReservationSlotState.INITIATED;

                CalendarSlot calendarSlot;
                if (_calendar.TryGetValue(slot, out calendarSlot))
                {
                    if (calendarSlot.State == CalendarSlotState.ASSIGNED)
                    {
                        state = ReservationSlotState.ABORTED;
                    }
                }
                else
                {
                    calendarSlot = new CalendarSlot();
                    calendarSlot.SlotNum = slot;
                    calendarSlot.State = CalendarSlotState.ACKNOWLEDGED;
                    calendarSlot.WaitingBook.Add(resID);

                    _calendar[slot] = calendarSlot;
                    Log.Debug(_userName, "Creating new calendar entry. Slot: " + calendarSlot.SlotNum + ". State: " + calendarSlot.State);
                }

                ReservationSlot rs = new ReservationSlot(resID, slot, state);
                reservationSlots.Add(rs);
            }

            //RELEASE LOCK HERE

            return reservationSlots;
        }

        private Reservation UpdateReservation(List<ReservationSlot> participantReply, string userID)
        {
            Reservation reservation;

            if (participantReply.Count > 0)
            {
                ReservationSlot slot = participantReply[0];
                if (!_activeReservations.TryGetValue(slot.ReservationID, out reservation))
                {
                    Log.Show(_userName, "WARN: Could not find reservation " + slot.ReservationID);
                    return null;
                }

            }
            else
            {
                return null;
            }

            foreach (ReservationSlot rSlot in participantReply)
            {
                if (rSlot.State == ReservationSlotState.ABORTED)
                {
                    AbortReservationSlot(GetSlot(reservation, rSlot.SlotID), false);
                } //if
            } //foreach

            reservation.Replied.Add(userID);

            return reservation;
        }

        private int RetrieveSequenceNumber()
        {
            int seqNumber = -1;

            while (seqNumber == -1)
            {
                try
                {
                    seqNumber = Helper.GetRandomServer(_servers).NextSequenceNumber();
                }
                catch (Exception)
                {
                    Console.WriteLine("\nCaught Exception Here!!\n");
                    //server has failed
                    //will try to get another server in next iteration
                }
            }

            return seqNumber;
        }

        private ReservationSlot GetSlotAndAbortPredecessors(Reservation res, int slotID)
        {
            foreach (ReservationSlot slot in res.Slots)
            {
                if (slot.SlotID == slotID)
                {
                    return slot;
                }
                else if (slot.State != ReservationSlotState.ABORTED)
                {
                    AbortReservationSlot(slot, false);
                }
            }

            return null;
        }

        private static ReservationSlot GetSlot(Reservation res, int slotID)
        {
            foreach (ReservationSlot slot in res.Slots)
            {
                if (slot.SlotID == slotID)
                {
                    return slot;
                }
            }

            return null;
        }

        private Reservation CreateReservation(ReservationRequest req, int resID, string initiatorID, string initiatorIP, int initiatorPort)
        {
            Reservation thisRes = new Reservation();
            thisRes.ReservationID = resID;
            thisRes.Description = req.Description;
            thisRes.Participants = req.Users;
            thisRes.InitiatorID = initiatorID;
            thisRes.InitiatorIP = initiatorIP;
            thisRes.InitiatorPort = initiatorPort;


            return thisRes;
        }
    }
}
