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
using System.Runtime.Remoting.Messaging;

namespace Server
{
    public interface IServer
    {
        void Init();
    }


    //TODO: maybe it's better to implement lookup service in a separate manager
    //it is here just for the purpose of testing
    public class Server : MarshalByRefObject, IServer
    {

        private string _username;
        private int _port;
        private string _puppetIP;
        private int _puppetPort;
        private List<ServerMetadata> _servers;
        private string _configFile;
        private string _path;
        ServerAction action;
        ServerFacade facade;
        ServerLookup lookup;
        PuppetMasterService pms;
        String connectionString;

        public Server(string filename)
        {
            _servers = new List<ServerMetadata>();
            ReadConfigurationFile(filename);
            action = new ServerAction(this._username);
            lookup = new ServerLookup(this._username, action, _servers);
            facade = new ServerFacade(this._username, _port, action, lookup);
        }

        public Server(string username, int port, string path, string configFile)
        {
            _username = username;
            _port = port;
            _path = path;
            _configFile = _path + configFile;
            _servers = new List<ServerMetadata>();
            ReadConfigurationFile();
            action = new ServerAction(_username);
            lookup = new ServerLookup(this._username, action, _servers);
            facade = new ServerFacade(this._username, _port,  action, lookup);

        }

        //Deprecated
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


            for (int i = 0; i < 2; i++)
            {
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

            for (int i = 0; i < 3; i++)
            {

                XmlNodeList server_ipportlist = serverslist[i].ChildNodes;
                string id = server_ipportlist[0].InnerText;
                if (_username.Equals(id))
                {
                    continue;
                }
                else
                {
                    string ip_addr = server_ipportlist[1].InnerText;
                    int port = Convert.ToInt32(server_ipportlist[2].InnerText);
                    ServerMetadata sm = new ServerMetadata();
                    sm.Username = id;
                    sm.IP_Addr = ip_addr;
                    sm.Port = port;
                    _servers.Add(sm);
                }
            }
        }

        /*
         * Implements IServer
         */

        void IServer.Init()
        {
            RegisterChannel();
            StartFacade();
            StartLookupServices(); //Should be done by the Connect method
            StartConsistencyService();
            initPMSObject();
            lookup.setPMS(pms); 
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
            string serviceName = _username + "/" + Common.Constants.SERVER_FACADE_SERVICE;
            Helper.StartService(_username, _port, serviceName, facade, typeof(IServerFacade));
        }

        void StartConsistencyService()
        {
            string serviceName = _username + "/" + Common.Constants.CONSISTENCY_SERVICE_NAME;
            Helper.StartService(_username, _port, serviceName, action, typeof(IConsistencyService));
        }

        void StartLookupServices()
        {

            string serviceName = _username + "/" + Common.Constants.LOOKUP_SERVICE_NAME;
            Helper.StartService(_username, _port, serviceName, lookup, typeof(ILookupService));
        }

        private void initPMSObject()
        {
            connectionString = "tcp://" + _puppetIP + ":" + _puppetPort + "/" + Common.Constants.PUPPET_MASTER_SERVICE_NAME;

            pms = (PuppetMasterService)Activator.GetObject(
                typeof(PuppetMasterService),
                connectionString);

        }

        private void NotifyPuppetMaster()
        {

           /* String connectionString = "tcp://" + _puppetIP + ":" + _puppetPort + "/" + Common.Constants.PUPPET_MASTER_SERVICE_NAME;

            pms = (PuppetMasterService)Activator.GetObject(
                typeof(PuppetMasterService),
                connectionString);
            */
            try
            {
                Log.Show(_username, "Trying to register to Puppet Master on: " + connectionString);
                pms.registerServer(_username, Helper.GetIPAddress(), _port);
                Log.Show(_username, "Sucessfully registered server on Pupper Master.");
                System.Console.ReadLine();
            }
            catch (SocketException)
            {
                Log.Show(_username, "Unable to connect to Puppet Master.");
            }
        }

    }
}