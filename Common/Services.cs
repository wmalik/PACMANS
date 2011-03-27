using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Beans;
using Common.Slots;
using System.Runtime.Remoting;


namespace Client.Services
{

    /* SERVICES EXPOSED TO THE PUPPET MASTER */

    public interface IFacadeService
    {
        bool Connect();

        bool Disconnect();

        Dictionary<int, CalendarSlot> ReadCalendar();

        bool CreateReservation(ReservationRequest reservation);
    }

    /* SERVICES EXPOSED TO THE CLIENTS */


    interface IBookingService
    {

        List<ReservationSlot> InitReservation(Reservation res);

        void BookSlot(int resID, int slotID);

        bool BookReply(int resID, int slotID, string userID);

        void PreCommit(int resId, int slotID);

        bool PreCommitReply(int resId, int slotID, string userID);

        void DoCommit(int resId, int slotID);

        bool DoCommitReply(int resId, int slotID, string userID);

        bool Abort(int resId, int slotID, string userID);

    }

}
