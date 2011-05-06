using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using Common.Beans;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using Common.Services;
using System.Net.Sockets;




namespace PuppetMaster
{
    public class PuppetMasterService : MarshalByRefObject
    {
        Hashtable clients_list = new Hashtable();
        Hashtable servers_list = new Hashtable();
        Hashtable clientFacadeList = new Hashtable();
        Hashtable serverFacadeList = new Hashtable();
        
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public PuppetMasterService()
        {
        }

        public PuppetMasterService(PuppetGUI gui)
        {
            this.Gui = gui;
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

        public Hashtable getServerFacadeList()
        {
            return this.serverFacadeList;
        }


        public void show(string msg)
        {
            Gui.show(msg);
        }

        private IClientFacade connectToClientFacadeService(ClientMetadata cm)
        {

            //connect to PuppetMaster here
            String connectionString = "tcp://" + cm.IP_Addr + ":" + cm.Port + "/" + cm.Username + "/" + Common.Constants.CLIENT_FACADE_SERVICE;

            IDictionary RemoteChannelProperties = new Dictionary<string, string>();
            RemoteChannelProperties["name"] = cm.Username;

            TcpChannel client_channel = new TcpChannel(RemoteChannelProperties, null, null);

            ChannelServices.RegisterChannel(client_channel, true);

            //TODO: uncomment and fix this to make it work
            IClientFacade facadeService = (IClientFacade)Activator.GetObject(
                typeof(IClientFacade),
                connectionString);


            return facadeService;


        }


        private IServerFacade connectToServerFacadeService(ServerMetadata sm)
        {

            String connectionString = "tcp://" + sm.IP_Addr + ":" + sm.Port + "/" + sm.Username + "/" + Common.Constants.SERVER_FACADE_SERVICE;

            IDictionary RemoteChannelProperties = new Dictionary<string, string>();
            RemoteChannelProperties["name"] = sm.Username;

            TcpChannel server_channel = new TcpChannel(RemoteChannelProperties, null, null);

            ChannelServices.RegisterChannel(server_channel, true);

            //TODO: uncomment and fix this to make it work
            IServerFacade facadeService = (IServerFacade)Activator.GetObject(
                typeof(IServerFacade),
                connectionString);

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

            IClientFacade facadeService = connectToClientFacadeService(cm);
            clientFacadeList.Add(username, facadeService);

            //update the Clients tree in PuppetGUI
            Gui.updateClientsTree(cm, null);

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
            IServerFacade isf = connectToServerFacadeService(sm);
            serverFacadeList.Add(username, isf);


            Gui.updateServersTree(sm, null);

            return true;
        }

        public void cleanUp()
        {
            /*
            getClientFacadeList().Clear();
            getServerFacadeList().Clear();
            getServersList().Clear();
            getClientsList().Clear();
            */
            clientFacadeList.Clear();
            serverFacadeList.Clear();
            servers_list.Clear();
            clients_list.Clear();
        }
    }
}

