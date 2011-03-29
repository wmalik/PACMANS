using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Util;
using Common.Beans;
using System.Xml;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using Common.Slots;
using Common.Services;
using PuppetMaster;
using System.Collections;
using Server.Services;

namespace Server
{
    public class ServerAction : MarshalByRefObject, IConsistencyService
    {
        private int _lastSeenSequenceNumber;

        public ServerAction()
        {
            _lastSeenSequenceNumber = 0;
        }

        //private IConsistencyService getInvokingServer(ServerMetadata server, string username)
        //{
        //    ServerMetadata chosenServer = server;
        //    String connectionString = "tcp://" + chosenServer.IP_Addr + ":" + chosenServer.Port + "/" + username + "/" + Common.Constants.CONSISTENCY_SERVICE_NAME;
        //    Log.Show(username, "Trying to find server: " + connectionString);

        //    IConsistencyService invokingServer = (IConsistencyService)Activator.GetObject(
        //        typeof(IConsistencyService),
        //        connectionString);

        //    return invokingServer;

        //}

        public bool WriteSequenceNumber(int seqNum)
        {
            Console.WriteLine("WriteSeqnum successfully invoked,{0}", seqNum);
            if (seqNum > _lastSeenSequenceNumber)
            {
                _lastSeenSequenceNumber = seqNum;

                return true;
            }

            else
            {
                //Yet to handle.
            }

            return false;
        }


        public int ReadSequenceNumber()
        {
            return 0;
        }

    }
}
