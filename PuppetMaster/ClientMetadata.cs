using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuppetMaster
{
    class ClientMetadata
    {
        private string username;
        private string ip_addr;
        private int port;
        private string service;

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

        public string Service
        {
            get;
            set;
        }



    }
}
