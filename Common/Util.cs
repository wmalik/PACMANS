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

            //TODO current implementation just get first server from list
            ServerMetadata chosenServer = servers[0];

            String connectionString = "tcp://" + chosenServer.IP_Addr + ":" + chosenServer.Port + "/" + chosenServer.Username + "/" + Common.Constants.LOOKUP_SERVICE_NAME;

            Log.Show("bla","Trying to lookup service: " + connectionString);

            ILookupService server = (ILookupService)Activator.GetObject(
                typeof(ILookupService),
                connectionString);


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

        public static void Show(string username, string msg)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("T") + "][" + username + "] " + msg);
        }
    }

}