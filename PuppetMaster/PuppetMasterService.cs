using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace PuppetMaster
{
    class PuppetMasterService : MarshalByRefObject
    {
        Hashtable clients_list = new Hashtable();
        Hashtable servers_list = new Hashtable();
        public void registerClient(string username, string ip_addr, int port, string service)
        {
            
            //creating a ClientMetadata object to store client information
            ClientMetadata cm = new ClientMetadata();
            cm.Username = username;
            cm.IP_Addr = ip_addr;
            cm.Port = port;
            cm.Service = service;
            
            //adding the client metadata to the global hashtable so that it can be used later on
            clients_list.Add(username, cm);
            
            //TODO: update the Clients tree in PuppetGUI
            //puppetgui.updateClientsTree(cm);
        }

        public void registerServer(string username, string ip_addr, int port, string service)
        {
            ServerMetadata sm = new ServerMetadata();
            sm.Username = username;
            sm.IP_Addr = ip_addr;
            sm.Port = port;
            sm.Service = service;

            //adding the client metadata to the global hashtable so that it can be used later on
            servers_list.Add(username, sm);

            //TODO: update the Servers tree in PuppetGUI
            //puppetgui.updateServersTree(sm);
        }
    }
}


/*
 Hashtable example
 * 
 * static Hashtable GetHashtable()
    {
	// Create and return new Hashtable.
	Hashtable hashtable = new Hashtable();
	hashtable.Add("Area", 1000);
	hashtable.Add("Perimeter", 55);
	hashtable.Add("Mortgage", 540);
	return hashtable;
    }

    static void Main()
    {
	Hashtable hashtable = GetHashtable();

	// See if the Hashtable contains this key.
	Console.WriteLine(hashtable.ContainsKey("Perimeter"));

	// Test the Contains method. It works the same way.
	Console.WriteLine(hashtable.Contains("Area"));

	// Get value of Area with indexer.
	int value = (int)hashtable["Area"];

	// Write the value of Area.
	Console.WriteLine(value);
    }
 
 */