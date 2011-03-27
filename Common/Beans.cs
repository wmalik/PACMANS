using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Common.Beans
{

    public class ReservationRequest
    {

        public string Description
        {
            get;
            set;
        }

        public string[] Users
        {
            get;
            set;
        }

        public int[] Slots
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

    public static class Helper
    {
        public static string getIPAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }
    }

}
