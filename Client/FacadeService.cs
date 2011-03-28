using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Services;
using Common.Slots;
using Common.Beans;

namespace Client
{
    public class FacadeService : MarshalByRefObject, IClientFacade
    {
        public bool Connect()
        {
            //let everyone know that I am going offline
            //set the isOnline flag to false
            //return;
            Console.WriteLine("Connected");

            return true;
        }

       public bool Disconnect()
        { 
            //let everyone know that I am going offline
            //set the isOnline flag to false
            //return;
            Console.WriteLine("Disconnected");
            return true;
        }


        public Dictionary<int, CalendarSlot> ReadCalendar()
        {
            //get the calendar from client object and return
            return null;
        }

        public bool CreateReservation(ReservationRequest reservation)
        {
            //initiate the reservation
            //setup callbacks
            //return;

            return true;
        }
    }
}
