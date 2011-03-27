using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Beans;
using System.Xml;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using Common.Slots;
using PuppetMaster;
using System.Collections;


namespace Client
{
    public interface IClient
    {
        void init();
    }

    public class Client : IClient
    {
        private Dictionary<int, CalendarSlot> calendar;
        private string Username;
        private int Port;
        private string PuppetIP;
        private int PuppetPort;
        private List<ServerMetadata> servers;
        private string filename;
        private FacadeService fc;

        public Client(string filename)
        {
            this.filename = filename;
        }

        public void init()
        {
            readConfigurationFile(this.filename);
            startFacadeService();
            connectToPuppetMaster();
        }

        private void readConfigurationFile(string filename)
        {
            string current_dir = System.IO.Directory.GetCurrentDirectory();
            //string filename = "Client.xml";
            XmlDocument xmlDoc = new XmlDocument(); //* create an xml document object.
            xmlDoc.Load(filename); //* load the XML document from the specified file.

            XmlNodeList usernamelist = xmlDoc.GetElementsByTagName("Username");
            XmlNodeList portlist = xmlDoc.GetElementsByTagName("Port");
            XmlNodeList puppetmasteriplist = xmlDoc.GetElementsByTagName("PuppetMasterIP");
            XmlNodeList puppetmasterportlist = xmlDoc.GetElementsByTagName("PuppetMasterPort");
            XmlNodeList serverslist = xmlDoc.GetElementsByTagName("Server");
            

            Username = usernamelist[0].InnerText; 
            Port = Convert.ToInt32(portlist[0].InnerText);
            PuppetIP = puppetmasteriplist[0].InnerText;
            PuppetPort = Convert.ToInt32(puppetmasterportlist[0].InnerText);
            servers = new List<ServerMetadata>();
            
            for (int i = 0; i < 4; i++)
            {
                XmlNodeList server_ipportlist = serverslist[i].ChildNodes;
                string ip_addr = server_ipportlist[0].InnerText;
                int port = Convert.ToInt32(server_ipportlist[1].InnerText);
                ServerMetadata sm = new ServerMetadata();
                sm.IP_Addr = ip_addr;
                sm.Port = port;
                servers.Add(sm);
            }
        }

        

        private void startFacadeService()
        {

            IDictionary RemoteChannelProperties = new Dictionary<string, string>();
            RemoteChannelProperties["port"] = Port.ToString();
            RemoteChannelProperties["name"] = Username+" FacadeService";
            TcpChannel channel = new TcpChannel(RemoteChannelProperties, null, null);
            //TcpChannel channel = new TcpChannel(Port);
            string user = Username;
            ChannelServices.RegisterChannel(channel, true);

            fc = new FacadeService();
            string service_name = Username + "/" + Common.Constants.FACADE_SERVICE_NAME;
            RemotingServices.Marshal(fc, service_name, typeof(FacadeService));


            /*
             RemotingConfiguration.RegisterWellKnownServiceType(
                 typeof(PuppetMasterService),
                 service,
                 WellKnownObjectMode.Singleton);
             */

            string service_string = "tcp://" + Helper.getIPAddress() + ":" + Port + "/" + service_name;
            show("Started " + service_string);

        }

        private void show(string msg)
        {
            Console.WriteLine("["+ Username +"] "+msg);
        }

        private void connectToPuppetMaster()
        {
            //connect to PuppetMaster here
            String connectionString = "tcp://" + PuppetIP + ":" + PuppetPort +"/"+ Common.Constants.PUPPET_MASTER_SERVICE_NAME;
            
            IDictionary RemoteChannelProperties = new Dictionary<string, string>();
            RemoteChannelProperties["port"] = (Port-10000).ToString();
            RemoteChannelProperties["name"] = Username;

            TcpChannel channel = new TcpChannel(RemoteChannelProperties, null, null);
           
            ChannelServices.RegisterChannel(channel, true);

            //TODO: uncomment and fix this to make it work
            PuppetMasterService pms = (PuppetMasterService)Activator.GetObject(
                typeof(PuppetMasterService),
                connectionString);
            
            try
            {
                //TODO: uncomment and fix this to make it work
                pms.registerClient(Username, Helper.getIPAddress(), Port);
                show("Connected to PuppetMaster on "+connectionString);
                System.Console.ReadLine();     
            }
            catch (SocketException)
            {
                show("Unable to connect to server");
                //textBox2.Text = "Unable to connect! bad monkey!";
            }
        }
    }
}
