﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Common.Services;
using Common.Beans;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.IO;
using System.Threading;

namespace Common.Util
{

    public static class Helper
    {
        public static string GetIPAddress()
        {
            //return "127.0.0.1";

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
            ILookupService server = null;
            String connectionString="";
            ServerMetadata chosenServer = null;

            while (server == null)
            {
                int server_num = new Random().Next(0, 3);

                chosenServer = servers[server_num];

                connectionString = "tcp://" + chosenServer.IP_Addr + ":" + chosenServer.Port + "/" + chosenServer.Username + "/" + Common.Constants.LOOKUP_SERVICE_NAME;

                try
                {
                    server = (ILookupService)Activator.GetObject(
                    typeof(ILookupService), connectionString);
                }
                catch (Exception)
                {
                    Log.Debug("Common", "Could not contact server, retrying in 100ms");
                }

                Thread.Sleep(100);
            }
            Console.WriteLine("Random server to contact is: " + connectionString);
            return server;
        }

        public static void StartService(string username, int port, string serviceName, MarshalByRefObject obj, Type requestedType)
        {
            RemotingServices.Marshal(obj, serviceName, requestedType);
            string serviceString = "tcp://" + GetIPAddress() + ":" + port + "/" + serviceName;
            Log.Show(username, "Started service: " + serviceString);
        }

        public static void StopService(string username, string objName, MarshalByRefObject obj)
        {
            if (RemotingServices.Disconnect(obj))
            {
                Log.Show(username, "Stopped service: " + objName);
            }
        }
    }

    public static class Log
    {

        static bool debug = true;

        public static void Show(string username, string msg)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("T") + "] [" + username + "] " + msg);
        }

        public static void Debug(string username, string msg)
        {
            if (debug)
            {
                Console.WriteLine("[" + DateTime.Now.ToString("T") + "][" + username + "] " + msg);
            }
        }

        public static void WriteToFile(StreamWriter logfile, string username, string msg)
        {
            string logmsg = "[" + DateTime.Now.ToString("T") + "][" + username + "] " + msg;
            logfile.WriteLine(logmsg);
            logfile.Flush();

        }

    }

}
