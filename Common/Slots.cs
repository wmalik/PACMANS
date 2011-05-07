using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Slots
{
    [Serializable]
    public class CalendarSlot
    {
        [NonSerialized]
        private bool m_locked;
        private List<int> m_waitingBook;

        public CalendarSlot()
        {
            WaitingBook = new List<int>();
            Participants = new List<string>();
            Locked = false;
        }

        public int SlotNum
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public List<string> Participants
        {
            get;
            set;
        }

        public CalendarSlotState State
        {
            get;
            set;
        }

        public int ReservationID
        {
            get;
            set;
        }

        public bool Locked
        {
            get { return this.m_locked; }
            set { this.m_locked = value; }
        }

        public List<int> WaitingBook
        {
            get { return this.m_waitingBook; }
            set { this.m_waitingBook = value; }
        }

        public override string ToString()
        {
            return "[" + SlotNum + 
                "|" + State +
                (State == CalendarSlotState.ACKNOWLEDGED ? "|" + String.Join(",",WaitingBook) : "") +
                (State == CalendarSlotState.ASSIGNED || State == CalendarSlotState.BOOKED ? "|res=" + ReservationID.ToString() : "") + 
                (State == CalendarSlotState.ASSIGNED ? "|" +  Description + "|" + String.Join(",", Participants) : "") + "]";
        }

    }

    [Serializable]
    public enum CalendarSlotState
    {
        FREE, ACKNOWLEDGED, BOOKED, ASSIGNED
    }

}
