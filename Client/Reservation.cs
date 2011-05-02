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
            this.ClientStubs = new Dictionary<string, IBookingService>();
            this.Replied = new List<string>();
            this.Aborted = false;
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

        public IBookingService InitiatorStub
        {
            get;
            set;
        }

        public bool Aborted
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

        public List<ReservationSlot> Slots
        {
            get;
            set;
        }

        public Dictionary<string, IBookingService> ClientStubs
        {
            get;
            set;
        }

        public List<string> Participants
        {
            get;
            set;
        }

        public List<string> Replied
        {
            get;
            set;
        }

        public int CurrentSlot
        {
            get;
            set;
        }

    }

    [Serializable]
    public class ReservationSlot
    {

        public ReservationSlot(int reservationID, int slotID, ReservationSlotState state)
        {
            this.ReservationID = reservationID;
            this.SlotID = slotID;
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

        public override string ToString()
        {
            return "Slot: " + SlotID + ". State: " + State;
        }


    }

    [Serializable]
    public enum ReservationSlotState
    {
        INITIATED, TENTATIVELY_BOOKED, COMMITTED, ABORTED
    }
}