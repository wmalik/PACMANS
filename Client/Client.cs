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


namespace Client
{
    interface IClient
    {
           
        //public void init();
    }

    class Client : IClient
    {
        private Dictionary<int, CalendarSlot> calendar;
        private string Username;
        private int Port;
        private string PuppetIP;
        private int PuppetPort;
        private List<ServerMetadata> servers;

        public Client(string filename)
        {
            readConfigurationFile(filename);
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

        void connectToPuppetMaster()
        {
            //connect to PuppetMaster here
            String connectionString = "tcp://" + PuppetIP + ":" + PuppetPort + Common.Constants.PUPPET_MASTER_SERVICE_NAME;
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

            /*TODO: uncomment and fix this to make it work
            PuppetMasterService pms = (PuppetMasterService)Activator.GetObject(
                typeof(PuppetMasterService),
                connectionString);
            */
            try
            {
                /*TODO: uncomment and fix this to make it work
                pms.registerServer("Worlds_First_PACMAN_Server", "203.89.2.3", 88822, "Wasifs_Dummy_Service");
                textBox2.Text = "PuppetMasterService called!";
                 * */
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
                //textBox2.Text = "Unable to connect! bad monkey!";
            }
        }
    }
}
