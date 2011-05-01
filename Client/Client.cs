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
using System.IO;


namespace Client
{
    public interface IClient
    {
        void Init();
    }

    public class Client : MarshalByRefObject, IClient, IClientFacade
    {

        private string _username; //c# convention for private vars, from http://10rem.net/articles/net-naming-conventions-and-programming-standards---best-practices :P
        private int _port;
        private string _puppetIP;
        private int _puppetPort;
        private List<ServerMetadata> _servers;
        PuppetMasterService pms;
        string connectionString;
        private SlotManager _slotManager;

        private bool _isOnline;

        private StreamWriter _logfile;
        private string _configFile;
        private string _path;


        public override object InitializeLifetimeService()
        {

            return null;

        }

        /*Deprecated*/
        public Client(string filename)
        {
            ReadConfigurationFile(filename);
            _isOnline = false;
            _slotManager = new SlotManager(_username, _port, _servers);
            _logfile = new StreamWriter(_path + "log\\log_client_" + _username + ".txt", true);
            _logfile.WriteLine("-");
            _logfile.AutoFlush = true;
        }


        public Client(string username, int port, string path, string configFile)
        {
            _username = username;
            _port = port;
            _path = path;
            _configFile = _path + configFile;
            ReadConfigurationFile();
            _isOnline = false;
            _slotManager = new SlotManager(_username, _port, _servers);
            string logpath = new Uri(_path + "log\\log_client_" + _username + ".txt").LocalPath;
            _logfile = new StreamWriter(logpath, true);
            _logfile.WriteLine("-");
            _logfile.AutoFlush = true;
        }

        /*Deprecated*/
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

            for (int i = 0; i < 1; i++)
            {
                //TODO: currently just reading first server
                XmlNodeList server_ipportlist = serverslist[i].ChildNodes;
                string id = server_ipportlist[0].InnerText;
                string ip_addr = server_ipportlist[1].InnerText;
                int port = Convert.ToInt32(server_ipportlist[2].InnerText);
                ServerMetadata sm = new ServerMetadata();
                sm.Username = id;
                sm.IP_Addr = ip_addr;
                sm.Port = port;
                _servers.Add(sm);
            }
        }

        /*Please use this method for reading the conf file*/
        private void ReadConfigurationFile()
        {
            string current_dir = System.IO.Directory.GetCurrentDirectory();
            XmlDocument xmlDoc = new XmlDocument(); //* create an xml document object.
            xmlDoc.Load(_configFile); //* load the XML document from the specified file.

            XmlNodeList puppetmasteriplist = xmlDoc.GetElementsByTagName("PuppetMasterIP");
            XmlNodeList puppetmasterportlist = xmlDoc.GetElementsByTagName("PuppetMasterPort");
            XmlNodeList serverslist = xmlDoc.GetElementsByTagName("Server");

            _puppetIP = puppetmasteriplist[0].InnerText;
            _puppetPort = Convert.ToInt32(puppetmasterportlist[0].InnerText);
            _servers = new List<ServerMetadata>();

            for (int i = 0; i < 3; i++)
            {
                //TODO: currently just reading first server
                XmlNodeList server_ipportlist = serverslist[i].ChildNodes;
                string id = server_ipportlist[0].InnerText;
                string ip_addr = server_ipportlist[1].InnerText;
                int port = Convert.ToInt32(server_ipportlist[2].InnerText);
                ServerMetadata sm = new ServerMetadata();
                sm.Username = id;
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
            StartFacade();
            initPMSObject();
            Connect();
            NotifyPuppetMaster();
        }

        private void initPMSObject()
        {
            connectionString = "tcp://" + _puppetIP + ":" + _puppetPort + "/" + Common.Constants.PUPPET_MASTER_SERVICE_NAME;

            pms = (PuppetMasterService)Activator.GetObject(
                typeof(PuppetMasterService),
                connectionString);

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
            string serviceName = _username + "/" + Common.Constants.CLIENT_FACADE_SERVICE;
            Helper.StartService(_username, _port, serviceName, this, typeof(IClientFacade));
            Log.WriteToFile(_logfile, _username, "Started Facade service");
        }

        void StartServices()
        {

            //Booking Service
            string serviceName = _username + "/" + Common.Constants.BOOKING_SERVICE_NAME;
            Helper.StartService(_username, _port, serviceName, _slotManager, typeof(IBookingService));
            Log.WriteToFile(_logfile, _username, "Started booking service");
        }

        void StopServices()
        {
            //Booking Service
            Helper.StopService(_username, "Booking service", _slotManager);
            Log.WriteToFile(_logfile, _username, "Stopped booking service");
        }

        private void NotifyPuppetMaster()
        {

            try
            {
                Log.Show(_username, "Trying to connect to Puppet Master on: " + connectionString);
                pms.registerClient(_username, Helper.GetIPAddress(), _port);
                Log.Show(_username, "Sucessfully registered client on Puppet Master.");
                Log.WriteToFile(_logfile, _username, "Sucessfully registered client on Puppet Master.");

                System.Console.ReadLine();
            }
            catch (SocketException)
            {
                Log.Show(_username, "Unable to connect to Puppet Master.");
                Log.WriteToFile(_logfile, _username, "Unable to connect to Puppet Master");
            }
        }



        /*
         * Implements IFacadeService
         */

        public bool Connect() //TODO: Just to test the server. Change it to IclientFacde.Connect() later.
        {

            if (!_isOnline)
            {
                
                _isOnline = true;
                StartServices();
                //Helper.GetRandomServer(_servers).RegisterUser(_username, Helper.GetIPAddress(), _port);
                Helper.GetRandomServer(_servers).NextSequenceNumber();

                //ILookupService ils = Helper.GetRandomServer(_servers);
                //Log.Show(_username, "%%%%%%%%%%% ils was acquired succesfully");
                //int seqnum = ils.NextSequenceNumber();  //Testing purpose. To be removed later.
                //Log.Show(_username, "%%%%%%%%%%% seqnum was acquired succesfully");
                
                /*Log.Show(_username, "Sequence number acquired: " + seqnum);
                Log.Show(_username, "Client is connected.");
                ClientMetadata cmd = Helper.GetRandomServer(_servers).Lookup(_username); //REMOVEME
                //pms.show("");
                if (cmd == null)
                {
                    pms.show("Client: "+_username + " no such user registered.");
                }
                else
                {
                    pms.show(cmd.IP_Addr + " is the ip of " + cmd.Username);
                    //Helper.GetRandomServer(_servers).UnregisterUser(_username);

                }
                Helper.GetRandomServer(_servers).UnregisterUser(_username);*/
                return true;
            }

            return false;
        }

        bool IClientFacade.Disconnect()
        {
            //pms.show(" ("+_username+")"+" I have received a Disconnect message");  
            if (_isOnline)
            {
                _isOnline = false;
                StopServices();
                //Broadcast offline information to initiators of ongoing reservations
                Log.Show(_username, "Client is disconnected.");
                return true;
            }


            return false;
        }


        List<CalendarSlot> IClientFacade.ReadCalendar()
        {
            return _slotManager.ReadCalendar();
        }

        bool IClientFacade.CreateReservation(ReservationRequest reservation)
        {


            return _slotManager.StartReservation(reservation);
        }

    }
}