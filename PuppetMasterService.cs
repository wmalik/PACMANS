using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuppetMaster
{
    class PuppetMasterService : MarshalByRefObject
    {
        public void registerClient(string username, string ip_addr)
        {
            //TODO: add to clients_list
            //TODO: update the Clients tree in PuppetGUI
        }

        public void registerServer(string username, string ip_addr)
        {
            //TODO: add to servers_list
            //TODO: update the Servers tree in PuppetGUI
        }
    }
}
