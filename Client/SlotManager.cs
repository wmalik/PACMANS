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

namespace Client
{

    public interface ISlotManager
    {
        bool StartReservation(ReservationRequest req);

        List<CalendarSlot> ReadCalendar();

        void AbortOngoingReservations();
    }

    public class SlotManager : MarshalByRefObject, IBookingService, ISlotManager
    {

        public delegate void InitReservationDelegate(ReservationRequest req, string ID, string initiatorIP, int initiatorPort);

        public delegate void ParticipantDelegate(int resID, int slotID);

        public delegate void InitiatorDelegate(int resID, int slotID, string userID, bool ack);

        public delegate void AbortDelegate(int resID);

        private Dictionary<int, CalendarSlot> _calendar;
        private Dictionary<int, Reservation> _activeReservations;
        private Dictionary<int, Reservation> _committedReservations;

        private string _userName;
        private int _port;

        private List<ServerMetadata> _servers;

        public SlotManager(string userName, int port, List<ServerMetadata> servers)
        {
            _calendar = new Dictionary<int, CalendarSlot>();
            _activeReservations = new Dictionary<int, Reservation>();
            _committedReservations = new Dictionary<int, Reservation>();
            _userName = userName;
            _port = port;
            _servers = servers;
        }

        /*
         * SLOT MANAGER METHODS
         */

        public bool StartReservation(ReservationRequest req)
        {
            //Updates request with sequence number
            req.ReservationID = RetrieveSequenceNumber();

            //Create and populate local reservation
            Reservation res = CreateReservation(req, _userName, Helper.GetIPAddress(), _port);
            

            //Mark slots initial states
            List<ReservationSlot> reservationSlots = CreateReservationSlots(req);

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
            if (req.Slots.Count == 0)
            {
                Log.Show(_userName, "No available slots on initiator, aborting reservation.");
                return false;
            }

            //just the initiator is on the participation
            if (req.Users.Count == 1)
            {
                bool assigned = false;

                foreach (ReservationSlot slot in res.Slots)
                {


                    if(slot.State != ReservationSlotState.ABORTED && _calendar[slot.SlotID].State != CalendarSlotState.ASSIGNED){
                        AssignCalendarSlot(res, slot);
                        assigned = true;
                        return true;
                    }
                    else if (slot.State != ReservationSlotState.ABORTED)
                    {
                        AbortReservationSlot(slot, true);
                    }
                }

                if (!assigned)
                {
                    AbortReservation(res.ReservationID);
                    return false;
                }
            }

            //Retrieve user's metadata
            List<ClientMetadata> onlineUsers = LookupUsers(req);

            if (onlineUsers.Count != (req.Users.Count - 1))
            {
                //TODO: Monitor offline nodes
                Log.Show(_userName, "Online users: " + onlineUsers.Count + ". Reservation users: " + req.Users.Count);
                Log.Show(_userName, "FATAL: Not all participants are online, aborting reservation for now.");
                return false;
            }

            //Add reservation to map of active reservations
            _activeReservations[res.ReservationID] = res;

            foreach (ClientMetadata clientMd in onlineUsers)
            {

                String connectionString = "tcp://" + clientMd.IP_Addr + ":" + clientMd.Port + "/" + clientMd.Username + "/" + Common.Constants.BOOKING_SERVICE_NAME;

                try
                {
                    IBookingService client = (IBookingService)Activator.GetObject(
                                                                            typeof(ILookupService),
                                                                            connectionString);
                    res.ClientStubs.Add(client);

                    InitReservationDelegate initRes = new InitReservationDelegate(client.InitReservation);
                    IAsyncResult RemAr = initRes.BeginInvoke(req, res.InitiatorID, res.InitiatorIP, res.InitiatorPort, null, null);

                }
                catch (SocketException e)
                {
                    Log.Show(_userName, "ERROR: Could not connect to client: " + clientMd.Username + ". Exception: " + e);
                    //TODO: do something?
                }
            }

            return true;
        }

        private List<ClientMetadata> LookupUsers(ReservationRequest req)
        {
            List<ClientMetadata> onlineUsers = new List<ClientMetadata>();
            ILookupService server = Helper.GetRandomServer(_servers);
            for (int i = 0; i < req.Users.Count; i++)
            {
                string userID = req.Users[i];
                if (!userID.Equals(_userName))
                {
                    try
                    {
                        ClientMetadata participantInfo = server.Lookup(userID);
                        onlineUsers.Add(participantInfo);
                    }
                    catch (SocketException)
                    {
                        //server has failed
                        //update server reference and decrease loop counter
                        server = Helper.GetRandomServer(_servers);
                        i--;
                    }
                }
            }
            return onlineUsers;
        }

        public List<CalendarSlot> ReadCalendar()
        {
            return _calendar.Values.ToList();
        }


        public void AbortOngoingReservations(){

            foreach (Reservation res in new List<Reservation>(_activeReservations.Values))
            {
                if (_userName.Equals(res.InitiatorID))
                {
                    AbortReservation(res.ReservationID);
                }
                else
                {
                    res.InitiatorStub.AbortReservation(res.ReservationID);
                }
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
            if (!_activeReservations.TryGetValue(resID, out res))
            {
                Log.Show(_userName, "WARN: Received book reply from unknown reservation " + resID);
                return;
            }

            res.Replied.Add(userID);

            if (!ack)
            {
                ReservationSlot slot = GetSlot(res, slotID);
                AbortReservationSlot(slot, false);
            }

            //Check if all participants replied before moving to next phase
            if (res.Replied.Count == (res.Participants.Count - 1))
            {
                Log.Show(_userName, "All participants replied for booking of slot " + slotID + " of reservation " + res.ReservationID);

                res.Replied.Clear();

                PrepareCommitOrBookNextSlot(res, slotID);
            }
        }

        void IBookingService.PrepareCommitReply(int resID, int slotID, string userID, bool ack)
        {

            Log.Show(_userName, "Received " + (ack ? "POSITIVE" : "NEGATIVE") + " pre commit ACK from participant " + userID + " for slot " + slotID + " of  reservation " + resID);

            Reservation res;
            if (!_activeReservations.TryGetValue(resID, out res))
            {
                Log.Show(_userName, "WARN: Received pre-commit reply from unknown reservation " + resID);
                return;
            }

            res.Replied.Add(userID);

            ReservationSlot slot = GetSlot(res, slotID);
            if (slot.State != ReservationSlotState.ABORTED && !ack)
            {
                AbortReservationSlot(slot, false);
            }

            //Check if all participants replied before moving to next phase
            if (res.Replied.Count == (res.Participants.Count - 1))
            {
                Log.Show(_userName, "All participants replied for pre-commit of slot " + slotID + " of reservation " + res.ReservationID);

                res.Replied.Clear();

                CommitOrBookNextSlot(res, slotID);
            }

        }

        /*
         * BOOKING SERVICE PATICIPANT
         */

        void IBookingService.InitReservation(ReservationRequest req, string initiatorID, string initiatorIP, int initiatorPort)
        {
            //Create and populate local reservation
            Reservation res = CreateReservation(req, _userName, initiatorIP, initiatorPort);

            //Mark slots initial states
            res.Slots = CreateReservationSlots(req);

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

            //if slot is already booked and resID is bigger than ID holding lock, add this request to queue
            if (calendarSlot.Locked || (calendarSlot.State == CalendarSlotState.BOOKED && resID > calendarSlot.ReservationID))
            {
                calendarSlot.WaitingBook.Remove(slotID);
                Log.Show(_userName, "Book request for slot " + slotID + " of  reservation " + resID + " was enqueued. Higher priority request " + calendarSlot.ReservationID + " already booked.");
                calendarSlot.BookQueue.Add(resID);
                return;
            }

            bool ack = calendarSlot.State != CalendarSlotState.ASSIGNED;

            if (ack)
            {
                BookCalendarSlot(res, resSlot);

            }
            else
            {
                resSlot.State = ReservationSlotState.ABORTED;
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

            //TODO: maybe also have a queue for do-commit requests? (may lead to deadlocks).. for now just denying 

            bool ack = calendarSlot.State == CalendarSlotState.BOOKED && calendarSlot.ReservationID == resID;

            if (ack)
            {
                LockCalendarSlot(res, resSlot);
            }
            else
            {
                resSlot.State = ReservationSlotState.ABORTED;
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

            AssignCalendarSlot(res, resSlot);

            //RELEASE LOCK HERE
        }

        public void AbortReservation(int resID)
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

            if (_userName == res.InitiatorID) {
                //I am the initiator, someone has disconnected in the middle of a reservation, tell the others
                foreach (IBookingService client in res.ClientStubs)
                {
                    try
                    {
                        AbortDelegate abortRes = new AbortDelegate(client.AbortReservation);
                        IAsyncResult RemAr = abortRes.BeginInvoke(resID, null, null);
                    }
                    catch (SocketException e)
                    {
                        Log.Show(_userName, "ERROR: Could not connect to client. Exception: " + e);
                        //TODO: do something
                    }
                }
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
                if (slot.State != ReservationSlotState.ABORTED && _calendar[slot.SlotID].State != CalendarSlotState.ASSIGNED)
                {
                    booked = true;

                    BookCalendarSlot(res, slot);

                    //RELEASE LOCK HERE

                    Log.Show(_userName, "Starting book process of slot " + slot.SlotID + " from reservation " + res.ReservationID);

                    foreach (IBookingService client in res.ClientStubs)
                    {
                        try
                        {
                            ParticipantDelegate bookSlot = new ParticipantDelegate(client.BookSlot);
                            IAsyncResult RemAr = bookSlot.BeginInvoke(res.ReservationID, slot.SlotID, null, null);
                        }
                        catch (SocketException e)
                        {
                            Log.Show(_userName, "ERROR: Could not connect to client. Exception: " + e);
                            //TODO: do something
                        }
                    }
                    break;
                }
            }

            if (!booked)
            {
                //RELEASE LOCK HERE

                Log.Show(_userName, "No available slots. Aborting reservation " + res.ReservationID);

                foreach (IBookingService client in res.ClientStubs)
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

            ReservationSlot slot = GetSlot(res, slotID);

            if (slot != null)
            {
                //GET LOCK HERE

                CalendarSlot calendarSlot = _calendar[slotID];

                if (slot.State == ReservationSlotState.ABORTED || calendarSlot.State == CalendarSlotState.ASSIGNED)
                {
                    Log.Show(_userName, "Booking of slot " + slotID + " of reservation " + res.ReservationID + " failed. Trying to book next slot.");
                    //RELEASE LOCK HERE

                    BookNextSlot(res);
                }
                else if (slot.State == ReservationSlotState.TENTATIVELY_BOOKED)
                {
                    Log.Show(_userName, "Slot " + slotID + " of reservation " + res.ReservationID + " was booked successfully. Starting commit process.");

                    LockCalendarSlot(res, slot);

                    //RELEASE LOCK HERE

                    foreach (IBookingService client in res.ClientStubs)
                    {
                        try
                        {
                            ParticipantDelegate preCommitSlot = new ParticipantDelegate(client.PrepareCommit);
                            IAsyncResult RemAr = preCommitSlot.BeginInvoke(res.ReservationID, slot.SlotID, null, null);
                        }
                        catch (SocketException e)
                        {
                            Log.Show(_userName, "ERROR: Could not connect to client. Exception: " + e);
                            //TODO: do something
                        }
                    }
                }
                else
                {
                    //RELEASE LOCK HERE
                    Log.Show(_userName, "FATAL: Undefined behavior... hopefully it will never happen.");
                }
            }
        }

        private void CommitOrBookNextSlot(Reservation res, int slotID)
        {
            ReservationSlot slot = GetSlot(res, slotID);

            if (slot != null)
            {
                //GET LOCK HERE

                CalendarSlot calendarSlot = _calendar[slotID];

                if (slot.State == ReservationSlotState.ABORTED || calendarSlot.State == CalendarSlotState.ASSIGNED)
                {
                    Log.Show(_userName, "Pre-commit of slot " + slotID + " of reservation " + res.ReservationID + " failed. Trying to book next slot.");
                    //RELEASE LOCK HERE

                    BookNextSlot(res);
                }
                else if (slot.State == ReservationSlotState.COMMITTED)
                {
                    Log.Show(_userName, "Slot " + slotID + " of reservation " + res.ReservationID + " was pre-committed successfully. Assigning calendar slot.");

                    AssignCalendarSlot(res, slot);

                    //RELEASE LOCK HERE

                    foreach (IBookingService client in res.ClientStubs)
                    {
                        try
                        {
                            ParticipantDelegate doCommitSlot = new ParticipantDelegate(client.DoCommit);
                            IAsyncResult RemAr = doCommitSlot.BeginInvoke(res.ReservationID, slot.SlotID, null, null);
                        }
                        catch (SocketException e)
                        {
                            Log.Show(_userName, "ERROR: Could not connect to client. Exception: " + e);
                            //TODO: do something
                        }
                    }

                    //TODO: Log to puppet master
                    Log.Show(_userName, "Reservation " + res.ReservationID + " succesfully assigned to slot " + slotID + " on clients: " + string.Join(",", res.Participants));
                }
                else
                {
                    //RELEASE LOCK HERE
                    Log.Show(_userName, "FATAL: Undefined behavior... hopefully it will never happen.");
                }
            }
        }

        private void AbortReservationSlot(ReservationSlot slot, bool locked)
        {
            Log.Debug(_userName, "Aborting slot " + slot.SlotID + " from reservation " + slot.ReservationID);

            if (slot != null)
            {
                slot.State = ReservationSlotState.ABORTED;
            }

            //IF NOT LOCKED GET LOCK HERE

            CalendarSlot cSlot = _calendar[slot.SlotID];

            cSlot.WaitingBook.Remove(slot.ReservationID);
            cSlot.BookQueue.Remove(slot.ReservationID);

            if (cSlot.State == CalendarSlotState.ACKNOWLEDGED && cSlot.WaitingBook.Count == 0)
            {
                cSlot.State = CalendarSlotState.FREE;
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

            //IF NOT LOCKED RELEASE LOCK HERE
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
        private void AssignCalendarSlot(Reservation res, ReservationSlot resSlot)
        {

            //Change calendar and reservation slot states
            CalendarSlot calendarSlot = _calendar[resSlot.SlotID];
            calendarSlot.State = CalendarSlotState.ASSIGNED;
            calendarSlot.ReservationID = res.ReservationID;

            //Remove from list of active reservations
            _activeReservations.Remove(res.ReservationID);
            _committedReservations[res.ReservationID] = res;

            //TODO: SEND ASYNCHRONOUS NACK TO ALL PENDING RESERVATIONS ON THE QUEUE
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
        }

        //Verify calendar slots and create reservation states
        private List<ReservationSlot> CreateReservationSlots(ReservationRequest req)
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
                    calendarSlot.WaitingBook.Add(req.ReservationID);

                    _calendar[slot] = calendarSlot;
                    Log.Debug(_userName, "Creating new calendar entry. Slot: " + calendarSlot.SlotNum + ". State: " + calendarSlot.State);
                }

                ReservationSlot rs = new ReservationSlot(req.ReservationID, slot, state);
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
                    GetSlot(reservation, rSlot.SlotID).State = ReservationSlotState.ABORTED;
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
                catch (SocketException)
                {
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

        private Reservation CreateReservation(ReservationRequest req, string initiatorID, string initiatorIP, int initiatorPort)
        {
            Reservation thisRes = new Reservation();
            thisRes.ReservationID = req.ReservationID;
            thisRes.Description = req.Description;
            thisRes.Participants = req.Users;
            thisRes.InitiatorID = initiatorID;
            thisRes.InitiatorIP = initiatorIP;
            thisRes.InitiatorPort = initiatorPort;
            

            return thisRes;
        }
    }
}
