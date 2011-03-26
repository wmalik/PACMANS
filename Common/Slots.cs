using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Slots
{
    public class CalendarSlot
    {

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

    }

    public enum CalendarSlotState
    {
        FREE, ACKNOWLEDGED, BOOKED, PRE_COMMITTED, ASSIGNED
    }
}
