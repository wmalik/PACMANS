using System.Collections.Generic;
using System;

namespace Common.Beans
{
    [Serializable]
    public class ReservationRequest
    {

        public int ReservationID
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public List<string> Users
        {
            get;
            set;
        }

        public List<int> Slots
        {
            get;
            set;
        }

    }

    [Serializable]
    public class ClientMetadata
    {

        public string Username
        {
            get;
            set;
        }

        public string IP_Addr
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }

    }

    [Serializable]
    public class ServerMetadata
    {

        public string Username
        {
            get;
            set;
        }

        public string IP_Addr
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }

    }

}
