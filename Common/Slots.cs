using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Slots
{
    [Serializable]
    public class CalendarSlot
    {
        private bool m_locked;
        private List<int> m_queue;
        private List<int> m_waitingBook;

        public CalendarSlot()
        {
            WaitingBook = new List<int>();
            BookQueue = new List<int>();
            Locked = false;
        }

        public int SlotNum
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

        public List<int> BookQueue
        {
            get { return this.m_queue; }
            set { this.m_queue = value; }
        }

        public List<int> WaitingBook
        {
            get { return this.m_waitingBook; }
            set { this.m_waitingBook = value; }
        }

        public override string ToString()
        {
            return "[" + SlotNum + "|" + State + "|reservation=" + (State == CalendarSlotState.ASSIGNED? ReservationID.ToString() : "X") + "]";
        }

    }

    [Serializable]
    public enum CalendarSlotState
    {
        FREE, ACKNOWLEDGED, BOOKED, ASSIGNED
    }

}
