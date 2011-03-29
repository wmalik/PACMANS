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

        public bool WriteSequenceNumber(int seqNum)
        {
            Console.WriteLine("WriteSeqnum successfully invoked,{0}", seqNum);
            return false;
        }


        public int ReadSequenceNumber()
        {
            return 0;
        }

    }
}
