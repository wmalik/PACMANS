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
using Client.Services;
using Server.Services;


namespace Client
{
    public interface IClient
    {
        void Init();
    }

    public class Client : MarshalByRefObject, IClient, IFacadeService
    {

        private string _username; //c# convention for private vars, from http://10rem.net/articles/net-naming-conventions-and-programming-standards---best-practices :P
        private int _port;
        private string _puppetIP;
        private int _puppetPort;
        private List<ServerMetadata> _servers;

        private ISlotManager _slotManager;

        private bool _isOnline;

        public Client(string filename)
        {
            ReadConfigurationFile(filename);
            _isOnline = false;
            _slotManager = new SlotManager(_username, _port, _servers);
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

            for (int i = 0; i < 3; i++)
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
         * Implements IClient
         */

        void IClient.Init()
        {
            RegisterChannel();
            RegisterServices();
            LookupRemoteServices();
            NotifyPuppetMaster();
        }

        private void RegisterChannel()
        {
            IDictionary RemoteChannelProperties = new Dictionary<string, string>();
            RemoteChannelProperties["port"] = _port.ToString();
            RemoteChannelProperties["name"] = _username ;
            TcpChannel channel = new TcpChannel(RemoteChannelProperties, null, null);
            ChannelServices.RegisterChannel(channel, true);
        }

        void RegisterServices()
        {

            //Facade Service
            string serviceName = _username + "/" + Common.Constants.FACADE_SERVICE_NAME;
            RemotingServices.Marshal(this, serviceName, typeof(IFacadeService));
            string serviceString = "tcp://" + Helper.GetIPAddress() + ":" + _port + "/" + serviceName;
            Log.Show(_username, "Started " + serviceString);
        }

        void LookupRemoteServices()
        {


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
                pms.registerClient(_username, Helper.GetIPAddress(), _port);
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

        bool IFacadeService.Connect()
        {
            if (!_isOnline)
            {
                _isOnline = true;
                //Helper.GetRandomServer(_servers).RegisterUser(_username, _port);
                Log.Show(_username, "Client is connected.");
                return true;
            }

            return false;
        }

        bool IFacadeService.Disconnect()
        { 
            if (_isOnline)
            {
                _isOnline = false;
                //Helper.GetRandomServer(_servers).UnregisterUser(_username);
                //Broadcast offline information to initiators of ongoing reservations
                Log.Show(_username, "Client is disconnected.");
                return true;
            }


            return false;
        }


        Dictionary<int, CalendarSlot> IFacadeService.ReadCalendar()
        {
            return null;
        }

         bool IFacadeService.CreateReservation(ReservationRequest reservation)
        {

            return _slotManager.StartReservation(reservation);
        }

    }
}