using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using Common.Beans;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using Client.Services;
using System.Net.Sockets;




namespace PuppetMaster
{
    public class PuppetMasterService : MarshalByRefObject
    {
        Hashtable clients_list = new Hashtable();
        Hashtable servers_list = new Hashtable();
        Hashtable clientFacadeList = new Hashtable();

        public PuppetMasterService()
        {
        }

        public PuppetMasterService(PuppetGUI gui)
        {
            this.Gui= gui;
        }

        public PuppetGUI Gui
        {
            get;
            set;
        }

        public Hashtable getClientsList()
        {
            return this.clients_list;
        }

        public Hashtable getServersList()
        {
            return this.servers_list;
        }

        public Hashtable getClientFacadeList()
        {
            return this.clientFacadeList;
        }

        private IFacadeService connectToClientFacadeService(ClientMetadata cm)
        {
            
            //connect to PuppetMaster here
            String connectionString = "tcp://" + cm.IP_Addr + ":" + cm.Port + "/" + cm.Username + "/"+Common.Constants.FACADE_SERVICE_NAME;

            IDictionary RemoteChannelProperties = new Dictionary<string, string>();
            //RemoteChannelProperties["port"] = (Port - 10000).ToString();
            RemoteChannelProperties["name"] = cm.Username;

            TcpChannel channel = new TcpChannel(RemoteChannelProperties, null, null);

            ChannelServices.RegisterChannel(channel, true);

            //TODO: uncomment and fix this to make it work
            IFacadeService facadeService = (IFacadeService)Activator.GetObject(
                typeof(IFacadeService),
                connectionString);

            try
            {
                //TODO: uncomment and fix this to make it work
                //pms.registerClient(Username, Helper.getIPAddress(), Port);
                //show("Connected to PuppetMaster on " + connectionString);
                //System.Console.ReadLine();
            }
            catch (SocketException)
            {
                //show("Unable to connect to server");
                //textBox2.Text = "Unable to connect! bad monkey!";
            }

            return facadeService;
             
             
        }
        public bool registerClient(string username, string ip_addr, int port)
        {
            
            //creating a ClientMetadata object to store client information
            ClientMetadata cm = new ClientMetadata();
            cm.Username = username;
            cm.IP_Addr = ip_addr;
            cm.Port = port;
            
            //adding the client metadata to the global hashtable so that it can be used later on
            clients_list.Add(username, cm);

            //TODO:
            IFacadeService facadeService = connectToClientFacadeService(cm);
            clientFacadeList.Add(username, facadeService);
            
            //update the Clients tree in PuppetGUI
            Gui.updateClientsTree(cm, null);

           // MessageBox.Show(username+" has joined!");

            return true;
        }

        public bool registerServer(string username, string ip_addr, int port)
        {
            ServerMetadata sm = new ServerMetadata();
            sm.Username = username;
            sm.IP_Addr = ip_addr;
            sm.Port = port;

            //adding the client metadata to the global hashtable so that it can be used later on
            servers_list.Add(username, sm);

            //TODO: update the Servers tree in PuppetGUI
            Gui.updateServersTree(sm, null);

            return true;
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