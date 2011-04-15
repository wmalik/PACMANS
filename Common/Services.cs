using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Beans;
using Common.Slots;
using System.Runtime.Remoting;


namespace Common.Services
{

    /* SERVICE EXPOSED FROM CLIENT TO THE PUPPER MASTER */
    public interface IClientCreator
    {
        bool CreateClient(string username, string ipAddr, int port);
    }

    public interface IClientFacade
    {
        bool Connect();

        bool Disconnect();

        Dictionary<int, CalendarSlot> ReadCalendar();

        bool CreateReservation(ReservationRequest reservation);
    }

    /* SERVICE EXPOSED FROM SERVER TO THE PUPPET MASTER */

    public interface IServerFacade
    {
        bool Connect();

        bool Disconnect();

    }

    /* SERVICE EXPOSED FROM SERVER TO THE CLIENTS */

    public interface ILookupService
    {

        void RegisterUser(string username, string ip, int port);

        void UnregisterUser(string username);

        ClientMetadata Lookup(string username);

        int NextSequenceNumber();

    }

}
