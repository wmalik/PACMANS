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
    public interface IServer
    {
        void Init();
    }


    //TODO: maybe it's better to implement lookup service in a separate manager
    //it is here just for the purpose of testing
    public class Server : MarshalByRefObject, IServer, IServerFacade, ILookupService
    {

        private string _username;
        private int _port;
        private string _puppetIP;
        private int _puppetPort;
        private List<ServerMetadata> _servers;


        //TODO: Temporary vars, move to some separate manager later
        private int _sequenceNumber;
        private Dictionary<string, ClientMetadata> _clients;


        private bool _isOnline;

        public Server(string filename)
        {
            _sequenceNumber = 0;
            _clients = new Dictionary<string, ClientMetadata>();
            ReadConfigurationFile(filename);
        }

        private void ReadConfigurationFile(string filename)
        {
            string current_dir = System.IO.Directory.GetCurrentDirectory();
            XmlDocument xmlDoc = new XmlDocument(); //* create an xml document object.
            xmlDoc.Load(filename); //* load the XML document from the specified file.

            XmlNodeList usernamelist = xmlDoc.GetElementsByTagName("Username");
            XmlNodeList portlist = xmlDoc.GetElementsByTagName("Port");
            XmlNodeList puppetmasteriplist = xmlDoc.GetElementsByTagName("PuppetMasterIP");
            XmlNodeList puppetmasterportlist = xmlDoc.GetElementsByTagName("PuppetMasterPort");
            XmlNodeList serverslist = xmlDoc.GetElementsByTagName("Server");

            _username = usernamelist[0].InnerText;
            _port = Convert.ToInt32(portlist[0].InnerText);
            _puppetIP = puppetmasteriplist[0].InnerText;
            _puppetPort = Convert.ToInt32(puppetmasterportlist[0].InnerText);
            _servers = new List<ServerMetadata>();

            for (int i = 0; i < 2; i++)
            {
                XmlNodeList server_ipportlist = serverslist[i].ChildNodes;
                string ip_addr = server_ipportlist[0].InnerText;
                int port = Convert.ToInt32(server_ipportlist[1].InnerText);
                ServerMetadata sm = new ServerMetadata();
                sm.IP_Addr = ip_addr;
                sm.Port = port;
                _servers.Add(sm);
            }
        }

        /*
         * Implements IServer
         */

        void IServer.Init()
        {
            RegisterChannel();
            //StartFacade(); //Cannot register two interfaces of the same object, so not exposing the facade services for now...
            StartServices(); //Should be done by the Connect method
            NotifyPuppetMaster();
        }

        private void RegisterChannel()
        {
            IDictionary RemoteChannelProperties = new Dictionary<string, string>();
            RemoteChannelProperties["port"] = _port.ToString();
            RemoteChannelProperties["name"] = _username;
            TcpChannel channel = new TcpChannel(RemoteChannelProperties, null, null);
            ChannelServices.RegisterChannel(channel, true);
        }

        void StartFacade()
        {

            //Facade Service
            string serviceName = _username + "/" + Common.Constants.FACADE_SERVICE_NAME;
            Helper.StartService(_username, _port, serviceName, this, typeof(IServerFacade));
        }

        void StartServices()
        {
            //Lookup Service
            //TODO: should have a separate object for this later on
            string serviceName = _username + "/" + Common.Constants.LOOKUP_SERVICE_NAME;
            Helper.StartService(_username, _port, serviceName, this, typeof(ILookupService));
        }

        void StopServices()
        {
            //Lookup Service
            Helper.StopService(_username, "Booking service", this);
        }

        private void NotifyPuppetMaster()
        {

            String connectionString = "tcp://" + _puppetIP + ":" + _puppetPort + "/" + Common.Constants.PUPPET_MASTER_SERVICE_NAME;

            PuppetMasterService pms = (PuppetMasterService)Activator.GetObject(
                typeof(PuppetMasterService),
                connectionString);
            
            try
            {
                Log.Show(_username, "Trying to connect to Pupper Master on: " + connectionString);
                pms.registerServer(_username, Helper.GetIPAddress(), _port);
                Log.Show(_username, "Sucessfully registered client on Pupper Master.");
                System.Console.ReadLine();
            }
            catch (SocketException)
            {
                Log.Show(_username, "Unable to connect to Pupper Master.");
                //textBox2.Text = "Unable to connect! bad monkey!";
            }
        }

        /*
         * Implements IFacadeService
         */

        bool IServerFacade.Connect()
        {
            if (!_isOnline)
            {
                _isOnline = true;
                StartServices();
                Log.Show(_username, "Server is connected.");
                return true;
            }

            return false;
        }

        bool IServerFacade.Disconnect()
        { 
            if (_isOnline)
            {
                _isOnline = false;
                //StopServices(); TODO: since facade and lookup are implemented by this same object, stopping it will probably stop both services
                Log.Show(_username, "Server is disconnected.");
                return true;
            }


            return false;
        }




        void ILookupService.RegisterUser(string username, string ip, int port)
        {
            ClientMetadata client = new ClientMetadata();
            client.IP_Addr = ip;
            client.Port = port;
            client.Username = username;

            Log.Show(_username, "Registered client " + username + ": " + ip + ":" + port);

            _clients[username] = client;
        }

        void ILookupService.UnregisterUser(string username)
        {
            Log.Show(_username, "Unregistered client: " + username);
            _clients.Remove(username);
        }

        ClientMetadata ILookupService.Lookup(string username)
        {
            ClientMetadata lookedUp;
            if (_clients.TryGetValue(username, out lookedUp)){
                Log.Show(_username, "Client info retrieved: " + username);
                return lookedUp;
            }

            Log.Show(_username, "No client info found: " + username);
            return null;
        }

        int ILookupService.NextSequenceNumber()
        {
            Log.Show(_username, "Sequence number retrieved. Next sequence number is: " + _sequenceNumber+1);

            return _sequenceNumber++;
        }
    }
}