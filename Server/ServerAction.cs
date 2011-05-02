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
using System.Threading;

namespace Server
{
    public class ServerAction : MarshalByRefObject, IConsistencyService
    {
        private int _lastSeenSequenceNumber;
        private string _username;
        private Dictionary<string, ClientMetadata> _clients;

        public ServerAction(string _username)
        {
            this._username = _username;
            _lastSeenSequenceNumber = 0;
            _clients = new Dictionary<string, ClientMetadata>();
        }

        
        public bool WriteSequenceNumber(int seqNum, string username)
        {
            Monitor.Enter(this);
            try
            {
                if (seqNum > _lastSeenSequenceNumber)
                {
                    Log.Show(_username, "[WRITE SEQ NUMBER] WriteSeqnum successful for sequence number: " + seqNum + "from SERVER:"+ username + " last seen sequence number: " + _lastSeenSequenceNumber);
                    _lastSeenSequenceNumber = seqNum;
                    return true;
                }
                else
                {
                    Log.Show(_username, "[WRITE SEQ NUMBER FAIL] WriteSeqnum failed for sequence number: " + seqNum + "from SERVER:" + username + " last seen sequence number: " + _lastSeenSequenceNumber);
                    return false;
                }
            }
            finally
            {
                Monitor.Exit(this);
            }
        }




        public bool WriteClientMetadata(ClientMetadata clientInfo)
        {
            Monitor.Enter(this);
            try
            {
                _clients[clientInfo.Username] = clientInfo;
                Log.Show(_username, "[REGISTER CLIENT WRITE] Client info registered for " + clientInfo.Username);
                return true;
            }
            finally
            {
                Monitor.Exit(this);
            }
        }


        public ClientMetadata ReadClientMetadata(string username)
        {
            ClientMetadata lookedUp;
            if (_clients.TryGetValue(username, out lookedUp))
            {
                Log.Show(_username, "[READ METADATA RETRIEVE] Client info retrieved is: " + username);
                return lookedUp;
            }

            Log.Show(_username, "[READ METADATA RETRIEVE] No client info found: " + username);
            return null;
        }


        public bool UnregisterUser(string username)
        {
            return (_clients.Remove(username));
        }

        public Dictionary<string, ClientMetadata> UpdateInfo()
        {
            return _clients;
        }

        public void setinfo(Dictionary<string, ClientMetadata> info)
        {
            _clients.Clear();
            foreach ( KeyValuePair<string, ClientMetadata> pair in info )
            {
                _clients.Add(pair.Key, pair.Value);
            }
        }
    }


}
