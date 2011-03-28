using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Common.Services;
using Common.Beans;

namespace Common.Util
{

    public static class Helper
    {
        public static string GetIPAddress()
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

        public static ILookupService GetRandomServer(List<ServerMetadata> servers)
        {
            return null;
        }
    }

    public static class Log
    {

        public static void Show(string username, string msg)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("T") + "][" + username + "] " + msg);
        }
    }

}
