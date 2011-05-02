using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Beans;
using System.Runtime.Remoting;


namespace Client.Services
{

    /* SERVICES EXPOSED TO THE CLIENTS */

    public interface IBookingService
    {

        void InitReservation(ReservationRequest req, int resID, string initiatorID, string initiatorIP, int initiatorPort);

        void InitReservationReply(List<ReservationSlot> slots, string userID);

        void BookSlot(int resID, int slotID);

        void BookReply(int resID, int slotID, string userID, bool ack);

        void PrepareCommit(int resId, int slotID);

        void PrepareCommitReply(int resId, int slotID, string userID, bool ack);

        void DoCommit(int resId, int slotID);

        void DoCommitReply(int resId, int slotID);

        void Disconnected(int resId, string userID);

        void AbortReservation(int resId);

        void FinishReservation(int resId);

    }

}