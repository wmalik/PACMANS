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

        Dictionary<int, CalendarSlot> ReadCalendar();
    }

    public class SlotManager :  MarshalByRefObject, IBookingService, ISlotManager
    {

        private Dictionary<int, CalendarSlot> _calendar;
        private Dictionary<int, Reservation> _activeReservations;

        private string _userName;
        private int _port;

        private List<ServerMetadata> _servers;

        public SlotManager(string userName, int port, List<ServerMetadata> servers)
        {
            _calendar = new Dictionary<int, CalendarSlot>();
            _activeReservations = new Dictionary<int, Reservation>();
            _userName = userName;
            _port = port;
            _servers = servers;
        }

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
                if (slot.State == ReservationSlotState.ABORTED) {
                    Log.Show(_userName, "Slot " + slot.SlotID + " not available on initiator. Removing from reservation.");
                    //removing slot of original request, since it will be passed to participants
                    reservationSlots.Remove(slot);
                    req.Slots.Remove(slot.SlotID);
                }
            }
            res.Slots = reservationSlots;

            //If no slots are available, cancel reservation
            if (req.Slots.Count == 0)
            {
                Log.Show(_userName, "No available slots on initiator, aborting reservation.");
                return false;
            }

            //Retrieve user's metadata
            List<ClientMetadata> onlineUsers = new List<ClientMetadata>();
            ILookupService server = Helper.GetRandomServer(_servers);
            for(int i=0; i<req.Users.Count; i++){
                try {
                    string userID = req.Users[i];
                    ClientMetadata participantInfo = server.Lookup(userID);
                } catch (SocketException) {
                    //server has failed
                    //update server reference and decrease loop counter
                    server = Helper.GetRandomServer(_servers);
                    i--;
                }
            }

            if (onlineUsers.Count != req.Users.Count)
            {
                //TODO: Monitor offline nodes
                Log.Show(_userName, "FATAL: Not all participants are online, aborting reservation for now.");
                return false;
            }

            foreach(ClientMetadata clientMd in onlineUsers){

                String connectionString = "tcp://" + clientMd.IP_Addr + ":" + clientMd.Port + "/" + clientMd.Username + "/" + Common.Constants.BOOKING_SERVICE_NAME;


                try{
                    IBookingService client = (IBookingService)Activator.GetObject(
                                                                            typeof(ILookupService),
                                                                            connectionString);

                    //TODO: make this asynchronous/parallel
                    List<ReservationSlot> participantReply = client.InitReservation(req, res.InitiatorID, res.InitiatorIP, res.InitiatorPort);
                    UpdateReservation(participantReply);
                } catch (SocketException){
                    Log.Show(_userName, "ERROR: Not all participants are online, aborting reservation for now.");
                    //TODO: do something
                }
            }
            
            //After feedback was received from all participants, send abort
            //foreach(KeyValuePair<int,ReservationSlot> rSlot in res.Slots){
            //}

            //Add reservation to map of active reservations
            _activeReservations[res.ReservationID] = res;

            return false;
        }

        private List<ReservationSlot> CreateReservationSlots(ReservationRequest req)
        {
            //Verify calendar slots and create reservation states
            List<ReservationSlot> reservationSlots = new List<ReservationSlot>();

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


                ReservationSlot rs = new ReservationSlot(req.ReservationID, slot, state);
                reservationSlots.Add(rs);
            }
            return reservationSlots;
        }

        private void UpdateReservation(List<ReservationSlot> participantReply)
        {
            foreach (ReservationSlot rSlot in participantReply)
            {
                if (rSlot.State == ReservationSlotState.ABORTED)
                {
                    Reservation reservation;
                    if (_activeReservations.TryGetValue(rSlot.ReservationID, out reservation))
                    {
                        reservation.Slots[rSlot.SlotID].State = ReservationSlotState.ABORTED;
                    }
                    else
                    {
                        Log.Show(_userName, "WARN: Could not find reservation " + rSlot.ReservationID);
                    } //else
                } //if
            } //foreach
        }

        private int RetrieveSequenceNumber()
        {
            int seqNumber = -1;

            while (seqNumber == -1)
            {
                try {
                    seqNumber = Helper.GetRandomServer(_servers).NextSequenceNumber();
                } catch (SocketException) {
                    //server has failed
                    //will try to get another server in next iteration
                }
            }

            return seqNumber;
        }

        public Dictionary<int, CalendarSlot> ReadCalendar()
        {
            throw new NotImplementedException();
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

        List<ReservationSlot> IBookingService.InitReservation(ReservationRequest req, string initiatorID, string initiatorIP, int initiatorPort)
        {
            throw new NotImplementedException();
        }

        void IBookingService.BookSlot(int resID, int slotID)
        {
            throw new NotImplementedException();
        }

        void IBookingService.BookReply(int resID, int slotID, string userID, bool ack)
        {
            throw new NotImplementedException();
        }

        void IBookingService.PreCommit(int resId, int slotID)
        {
            throw new NotImplementedException();
        }

        void IBookingService.PreCommitReply(int resId, int slotID, string userID, bool ack)
        {
            throw new NotImplementedException();
        }

        void IBookingService.DoCommit(int resId, int slotID)
        {
            throw new NotImplementedException();
        }

        void IBookingService.DoCommitReply(int resId, int slotID, string userID, bool ack)
        {
            throw new NotImplementedException();
        }

        bool IBookingService.Abort(int resId, int slotID, string userID)
        {
            throw new NotImplementedException();
        }

    }
}
