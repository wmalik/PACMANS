using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Client.Services;

namespace Client
{
    public class Reservation
    {

        public Reservation()
        {
            this.UserStubs = new Dictionary<string, IClient>();
            this.Slots = new Dictionary<int, ReservationSlot>();
        }

        public int ReservationID
        {
            get;
            set;
        }

        public string InitiatorID
        {
            get;
            set;
        }

        //FIXME: don't know if initiatorIP and initiatorPort are needed, keeping just in case

        public string InitiatorIP
        {
            get;
            set;
        }

        public int InitiatorPort
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public Dictionary<int, ReservationSlot> Slots
        {
            get;
            set;
        }

        public Dictionary<string, IClient> UserStubs
        {
            get;
            set;
        }

        public List<string> Participants
        {
            get;
            set;
        }


    }

    public class ReservationSlot
    {

        public ReservationSlot(int reservationID, int slotID, ReservationSlotState state)
        {
            this.ReservationID = reservationID;
            this.SlotID = SlotID;
            this.State = state;
        }

        public int ReservationID
        {
            get;
            set;
        }

        public int SlotID
        {
            get;
            set;
        }

        public ReservationSlotState State
        {
            get;
            set;
        }

    }

    public enum ReservationSlotState
    {
        INITIATED, TENTATIVELY_BOOKED, PRE_COMMITTED, COMMITTED, ABORTED
    }
}
