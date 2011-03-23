using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;


//added by me
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using System.Net;

namespace PuppetMaster
{
    public partial class PuppetGUI : Form
    {
        string portnum = "";
        string service = "";
        public PuppetGUI()
        {
            InitializeComponent();
        }

        private void readConfigurationFile()
        {
            string current_dir = System.IO.Directory.GetCurrentDirectory();
            string filename = "puppetmaster.xml";
            XmlDocument xmlDoc = new XmlDocument(); //* create an xml document object.
            xmlDoc.Load(filename); //* load the XML document from the specified file.
            
            XmlNodeList portnodelist = xmlDoc.GetElementsByTagName("port");
            XmlNodeList servicenodelist = xmlDoc.GetElementsByTagName("service");
            
          portnum = portnodelist[0].InnerText;
          service = servicenodelist[0].InnerText;

          //show("Started listening on port " + portnum);
          //show("Service name: "+service);
        }

        private void show(string msg)
        {
            this.consoleBox.AppendText("\r\n"+"(*) "+msg);
        }

        private string getIPAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        private void startPuppetMasterService()
        {
            int port = Convert.ToInt32(portnum);
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, true);

            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PuppetMasterService),
                "PuppetMasterService",
                WellKnownObjectMode.Singleton);

            string service_string = "tcp://" + getIPAddress() + ":" + port + "/" + service;
            show("Started "+service_string);
            
        }

        

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void reallyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: fetch selected username
            //TODO: get ip_addr, port, service of the selected username from hashtable or dict
            //TODO: connect to the service (if not already connected)
            //TODO: call the connectClient(args) method of the remote object 
        }

        private void PuppetGUI_Shown(object sender, EventArgs e)
        {
            readConfigurationFile();
            startPuppetMasterService();
            
        }

        private void createRes_Click(object sender, EventArgs e)
        {
            //TODO: fetch selected username
            //TODO: get ip_addr, port, service of the first username from hashtable or dict
            //TODO: connect to the service
            //TODO: call the createReservation(args) method of the remote object 
        }

        private void disconnectMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: fetch selected username
            //TODO: get ip_addr, port, service of the selected username from hashtable or dict
            //TODO: connect to the service (if not already connected)
            //TODO: call the disconnectClient(args) method of the remote object 
        }

        private void readCalMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: fetch selected username
            //TODO: get ip_addr, port, service of the selected username from hashtable or dict
            //TODO: connect to the service (if not already connected)
            //TODO: get the Calendar state by calling readCalendar(args) method of the remote object
        }
    }
}
