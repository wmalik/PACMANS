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
using System.Collections;

using Common.Beans;

namespace PuppetMaster
{
    public partial class PuppetGUI : Form
    {
        string portnum = "";
        string service = "";
        PuppetMasterService pms;

        public PuppetGUI()
        {
            InitializeComponent();
        }

        private void readConfigurationFile()
        {
            string current_dir = System.IO.Directory.GetCurrentDirectory();
            string filename = "config/puppetmaster.xml";
            XmlDocument xmlDoc = new XmlDocument(); //* create an xml document object.
            xmlDoc.Load(filename); //* load the XML document from the specified file.
            
            XmlNodeList portnodelist = xmlDoc.GetElementsByTagName("port");
            XmlNodeList servicenodelist = xmlDoc.GetElementsByTagName("service");
            
          portnum = portnodelist[0].InnerText; //portnum where PuppetMaster listens for incoming requests
          service = servicenodelist[0].InnerText; //name of the Remoting service

          
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

            
            pms = new PuppetMasterService();
            pms.Gui = this;
            RemotingServices.Marshal(pms,service, typeof(PuppetMasterService));
            
            
           /*
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PuppetMasterService),
                service,
                WellKnownObjectMode.Singleton);
            */

            string service_string = "tcp://" + getIPAddress() + ":" + port + "/" + service;
            show("Started "+service_string);
            
        }

        //developed for debugging
        public void updateClientsTree()
        {
 
        TreeNode newNode = new TreeNode("NewClient");
        //treeView1.SelectedNode.Nodes.Add(newNode);
        int index = treeView1.Nodes.IndexOfKey("Clients");
        treeView1.Nodes[index].Nodes.Insert(0, "Client Wongo");
      
        }

        //used for adding clients in the treeview
        public void updateClientsTree(object sender, EventArgs ea)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, EventArgs>(updateClientsTree), sender, ea);
                return;
            }

            ClientMetadata cm = (ClientMetadata)sender;
            TreeNode newNode = new TreeNode(cm.Username);
            int index = treeView1.Nodes.IndexOfKey("Clients");
            treeView1.Nodes[index].Nodes.Insert(0, newNode);
            show(cm.Username + " just came online");

        }

      

        //used for adding servers in the treeview
        public void updateServersTree(object sender, EventArgs ea)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, EventArgs>(updateServersTree), sender, ea);
                return;
            }
            ServerMetadata sm = (ServerMetadata)sender;
            TreeNode newNode = new TreeNode(sm.Username);
            //treeView1.SelectedNode.Nodes.Add(newNode);
            int index = treeView1.Nodes.IndexOfKey("Servers");
            treeView1.Nodes[index].Nodes.Insert(0, newNode);
            show(sm.Username + " just came online");

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

            //fetch selected username
            string username = treeView1.SelectedNode.Text;
            string parent = treeView1.SelectedNode.Parent.Text;
            string ip_addr;
            int port;
            string service;

            //get ip_addr, port, service of the selected server (or client) from hashtable or dict
            if (parent.Equals("Servers"))
            {
                ServerMetadata sm = (ServerMetadata)pms.getServersList()[username];
                ip_addr = sm.IP_Addr;
                port = sm.Port;
                service = sm.Service;
            }

            else if (parent.Equals("Clients"))
            {
                ClientMetadata cm = (ClientMetadata)pms.getClientsList()[username];
                ip_addr = cm.IP_Addr;
                port = cm.Port;
                service = cm.Service;
            }
            else
            {
                return;
            }
            //TODO: connect to the service (if not already connected)
            show("TODO: connect to the service (if not already connected)");
            //TODO: call the disconnectClient(args) method of the remote object 
            show("TODO: call the disconnectClient(args) method of the remote object");
            show("IP:"+ip_addr+" port:"+port+" service:"+service);
            //TODO:
            show("TODO: remove the server from tree");
            treeView1.SelectedNode.Remove();

        }

        private void readCalMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: fetch selected username
            //TODO: get ip_addr, port, service of the selected username from hashtable or dict
            //TODO: connect to the service (if not already connected)
            //TODO: get the Calendar state by calling readCalendar(args) method of the remote object
        }

        private void connectMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: fetch selected username
            //TODO: get ip_addr, port, service of the selected username from hashtable or dict
            //TODO: connect to the service (if not already connected)
            //TODO: call the connectClient(args) method of the remote object 
        }
    }
}
