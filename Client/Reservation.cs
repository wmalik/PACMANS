using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
    class Reservation
    {

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

        public List<string> Users
        {
            get;
            set;
        }


    }

    public class ReservationSlot
    {

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
