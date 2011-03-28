using System.Collections.Generic;

namespace Common.Beans
{

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
