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
    class ServerFacade : MarshalByRefObject, IServerFacade
    {
        private string _username;
        private bool _isOnline;
        ServerAction action;
        ServerLookup lookup;

        public ServerFacade(string username, ServerAction action, ServerLookup lookup)
        {
            this._username = username;
            this.action = action;
            this.lookup = lookup;
            _isOnline = true;
        }

        public bool Connect()
        {
            if (!_isOnline)
            {
                _isOnline = true;
                //   StartServices();
                Log.Show(_username, "Server is connected.");
                return true;
            }

            return false;
        }

        public bool Disconnect()
        {
            if (_isOnline)
            {
                _isOnline = false;
                StopServices(); 
                Log.Show(_username, "Server is disconnected.");
                return true;
            }

            return false;       
        }

        void StopServices()
        {
            //Lookup Service
            Helper.StopService(_username, "Lookup service", lookup);
            Helper.StopService(_username, "Consistency service", action);
        }
    }
}
