using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Beans;
using Common.Slots;
using System.Runtime.Remoting;


namespace Client.Services
{

    /* SERVICES EXPOSED TO THE CLIENTS */


    public interface IBookingService
    {

        List<ReservationSlot> InitReservation(ReservationRequest req, string initiatorID, string initiatorIP, int initiatorPort);

        void BookSlot(int resID, int slotID);

        void BookReply(int resID, int slotID, string userID, bool ack);

        void PreCommit(int resId, int slotID);

        void PreCommitReply(int resId, int slotID, string userID, bool ack);

        void DoCommit(int resId, int slotID);

        void DoCommitReply(int resId, int slotID, string userID, bool ack);

        bool Abort(int resId, int slotID, string userID);

    }

}
