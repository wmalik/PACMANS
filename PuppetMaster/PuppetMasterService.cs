using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuppetMaster
{
    class PuppetMasterService : MarshalByRefObject
    {
        public void registerClient(string username, string ip_addr, int port, string service)
        {
            ClientMetadata cm = new ClientMetadata();
            cm.Username = username;
            cm.IP_Addr = ip_addr;
            cm.Port = port;
            cm.Service = service;
            //TODO: add the cm object to clients_list

            //TODO: update the Clients tree in PuppetGUI
        }

        public void registerServer(string username, string ip_addr, int port, string service)
        {
            ServerMetadata sm = new ServerMetadata();
            sm.Username = username;
            sm.IP_Addr = ip_addr;
            sm.Port = port;
            sm.Service = service;
            //TODO: add the sm object to servers_list

            //TODO: update the Servers tree in PuppetGUI
        }
    }
}
