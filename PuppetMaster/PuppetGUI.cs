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
using Common.Services;
using Common.Slots;
using System.Diagnostics;
using System.Threading;

namespace PuppetMaster
{
    public partial class PuppetGUI : Form
    {
        string portnum = "";
        string service = "";
        string client_dir = "";
        string server_dir = "";
        string invisible_windows = "";
        PuppetMasterService pms;
        OpenFileDialog openFileDialog1;
        List<Process> processes = new List<Process>();

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
            XmlNodeList clientdirnodelist = xmlDoc.GetElementsByTagName("client_dir");
            XmlNodeList serverdirnodelist = xmlDoc.GetElementsByTagName("server_dir");
            XmlNodeList invisiblenodelist = xmlDoc.GetElementsByTagName("invisible_windows");

            portnum = portnodelist[0].InnerText; //portnum where PuppetMaster listens for incoming requests
            service = servicenodelist[0].InnerText; //name of the Remoting service
            client_dir = clientdirnodelist[0].InnerText; //
            server_dir = serverdirnodelist[0].InnerText; //
            invisible_windows = invisiblenodelist[0].InnerText; //


        }

        delegate void Invoker(object parameter);

        public void show(object sender)
        {

            if (this.InvokeRequired)
            {

                this.BeginInvoke(new Invoker(show), sender);
                return;
            }

            string msg = (string)sender;
            this.consoleBox.AppendText("\r\n" + "(*) " + msg);
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
            IDictionary RemoteChannelProperties = new Dictionary<string, string>();
            RemoteChannelProperties["name"] = "PuppetMasterChannel";
            RemoteChannelProperties["port"] = port.ToString();
            TcpChannel channel = new TcpChannel(RemoteChannelProperties, null, null);
            ChannelServices.RegisterChannel(channel, true);


            pms = new PuppetMasterService();
            pms.Gui = this;
            RemotingServices.Marshal(pms, Common.Constants.PUPPET_MASTER_SERVICE_NAME, typeof(PuppetMasterService));


            /*
             RemotingConfiguration.RegisterWellKnownServiceType(
                 typeof(PuppetMasterService),
                 service,
                 WellKnownObjectMode.Singleton);
             */

            string service_string = "tcp://" + getIPAddress() + ":" + port + "/" + Common.Constants.PUPPET_MASTER_SERVICE_NAME;
            show("Started " + service_string);

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
            newNode.Name = cm.Username;
            int index = treeView1.Nodes.IndexOfKey("Clients");
            treeView1.Nodes[index].Nodes.Insert(0, newNode);
            newNode.Parent.Expand();
            show(cm.Username + " (" + cm.IP_Addr + ":" + cm.Port + ") just came online");

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
            newNode.Name = sm.Username;
            int index = treeView1.Nodes.IndexOfKey("Servers");
            treeView1.Nodes[index].Nodes.Insert(0, newNode);
            newNode.Parent.Expand();
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
            string initiator = usersBox.Text.Split(',')[0];
            List<string> usersList = usersBox.Text.Split(',').ToList<string>();
            List<int> slotsList = slotsBox.Text.Split(',').ToList().ConvertAll<int>(Convert.ToInt32);
            string desc = descBox.Text;

            ReservationRequest rr = new ReservationRequest();
            rr.Description = desc;
            rr.Users = usersList;
            rr.Slots = slotsList;
            IClientFacade icf = (IClientFacade)pms.getClientFacadeList()[initiator];
            try
            {
                if (icf != null)
                    icf.CreateReservation(rr);  //There is an exception unhandled here.
                else
                    show("Unable to get Client Facade of Initiator");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[PMS-EXCEPTION] " + ex.ToString());
            }

        }

        private void disconnectMenuItem_Click(object sender, EventArgs e)
        {

            //fetch selected username
            string username = treeView1.SelectedNode.Text.Trim();

            if (username.Equals("Servers") || username.Equals("Clients"))
                return;

            string parent = treeView1.SelectedNode.Parent.Text;

            if (parent.Equals("Servers"))
            {
                ServerMetadata sm = (ServerMetadata)pms.getServersList()[username];
                IServerFacade isf = (IServerFacade)pms.getServerFacadeList()[username];
                isf.Disconnect(); /*commented because Server Facade is not implemented yet*/
                //show(username + " has been disconnected");
                treeView1.SelectedNode.ImageIndex = 1;
                treeView1.SelectedNode.SelectedImageIndex = 1;
            }

            else if (parent.Equals("Clients"))
            {
                ClientMetadata cm = (ClientMetadata)pms.getClientsList()[username];
                IClientFacade fs = (IClientFacade)pms.getClientFacadeList()[username];
                fs.Disconnect();
                show(username + " has been disconnected");
                treeView1.SelectedNode.ImageIndex = 1;
                treeView1.SelectedNode.SelectedImageIndex = 1;

            }
            else
            {
                return;
            }

        }

        private void readCalMenuItem_Click(object sender, EventArgs e)
        {

            //fetch selected username
            string username = treeView1.SelectedNode.Text;

            if (username.Equals("Servers") || username.Equals("Clients"))
                return;

            string parent = treeView1.SelectedNode.Parent.Text;

            if (parent.Equals("Servers"))
            {
                MessageBox.Show("You are messing with me rite?");
            }

            else if (parent.Equals("Clients"))
            {
                ClientMetadata cm = (ClientMetadata)pms.getClientsList()[username];
                IClientFacade fs = (IClientFacade)pms.getClientFacadeList()[username];
                List<CalendarSlot> calendar = fs.ReadCalendar();

                show("Calendar for " + username + ": " + string.Join(",", calendar));

            }
            else
            {
                return;
            }



        }

        private void connectMenuItem_Click(object sender, EventArgs e)
        {

            //fetch selected username
            string username = treeView1.SelectedNode.Text;

            if (username.Equals("Servers") || username.Equals("Clients"))
                return;

            string parent = treeView1.SelectedNode.Parent.Text;


            if (parent.Equals("Servers"))
            {
                ServerMetadata sm = (ServerMetadata)pms.getServersList()[username];
                IServerFacade isf = (IServerFacade)pms.getServerFacadeList()[username];
                isf.Connect(); /*commented because Server Facade is not implemented yet*/
                //show(username + " is now connected");
                treeView1.SelectedNode.ImageIndex = 0;
                treeView1.SelectedNode.SelectedImageIndex = 0;
            }

            else if (parent.Equals("Clients"))
            {
                ClientMetadata cm = (ClientMetadata)pms.getClientsList()[username];
                IClientFacade fs = (IClientFacade)pms.getClientFacadeList()[username];
                fs.Connect(); //EXCEPTION UNHANDLED - WASIF CHECK THIS
                show(username + " has been connected");
                treeView1.SelectedNode.ImageIndex = 0;
                treeView1.SelectedNode.SelectedImageIndex = 0;

            }
            else
            {
                return;
            }


        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void loadEventsButton_Click(object sender, EventArgs e)
        {


            openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "txt files (*.txt)|*.txt";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //creating a new thread to read the event file
                Thread fileReadingThread = new Thread(readEventFileAndDispatchEvents);
                fileReadingThread.Start();
            }


        }

        private void readEventFileAndDispatchEvents()
        {
            string line = string.Empty;
            System.IO.StreamReader sr = new
               System.IO.StreamReader(openFileDialog1.FileName);
            while ((line = sr.ReadLine()) != null)
            {
                show(line);
                string opcode = line.Split(' ')[0];
                string username;
                IClientFacade icf;
                IServerFacade sf;
                List<CalendarSlot> clientCalendar;
                //TODO: fetch initiator from Users list

                switch (opcode)
                {
                    case "disconnect":
                        username = line.Split(' ')[1];

                        if (username.StartsWith("central"))
                        {
                            ServerMetadata sm = (ServerMetadata)pms.getServersList()[username];
                            IServerFacade isf = (IServerFacade)pms.getServerFacadeList()[username];
                            while (isf == null)
                            {
                                show("Trying to disconnect server: " + username);
                                isf = (IServerFacade)pms.getServerFacadeList()[username];
                                Thread.Sleep(100);
                            }

                            try
                            {
                                isf.Disconnect();
                                changeIconToDisconnected(username, null);
                                show("Server disconnected. TODO: call server facade");
                            }
                            catch (Exception ioe)
                            {
                                MessageBox.Show(ioe.ToString());
                            }
                        }
                        else
                        {
                            icf = (IClientFacade)pms.getClientFacadeList()[username];
                            while (icf == null)
                            {
                                show("Trying to disconnect client: " + username);
                                icf = (IClientFacade)pms.getClientFacadeList()[username];
                                Thread.Sleep(1000);
                            }
                            icf.Disconnect();

                            changeIconToDisconnected(username, null);
                        }
                        break;


                    case "connect":

                        username = line.Split(' ')[1].Trim();
                        string ip = line.Split(' ')[2].Split(':')[0].Trim();
                        int port = Convert.ToInt32(line.Split(' ')[2].Split(':')[1]);

                        icf = (IClientFacade)pms.getClientFacadeList()[username];
                        sf = (IServerFacade)pms.getServerFacadeList()[username];

                        if (icf != null) //means the client has already been created
                        {
                            show("Trying to reconnect to client: " + username);
                            icf.Connect();
                            changeIconToConnected(username, null);
                        }

                        else if (sf != null)
                        {
                            show("Reconnecting to server: " + username);
                            sf.Connect();
                            changeIconToConnected(username, null);
                        }
                        else //means a new process needs to be started
                        {

                            if (username.StartsWith("central"))
                            {
                                string path = server_dir;
                                ProcessStartInfo startInfo = new ProcessStartInfo();


                                startInfo.FileName = path + "Server.exe";
                                startInfo.Arguments = username + " " + port;
                                Process process = new Process();
                                process.StartInfo = startInfo;
                                process.Start();
                                processes.Add(process);


                                //TODO: save the PIDS of all processes and kill them on exit

                                //if server  is online, we move on to the next event. If not, we wait until the server is online
                                IServerFacade isf = (IServerFacade)pms.getServerFacadeList()[username];
                                while (isf == null)
                                {
                                    Thread.Sleep(500);
                                    isf = (IServerFacade)pms.getServerFacadeList()[username];
                                }

                            }
                            else //means its a client
                            {
                                string path = client_dir;
                                ProcessStartInfo startInfo = new ProcessStartInfo();

                                startInfo.FileName = path + "Client.exe";
                                startInfo.Arguments = username + " " + port;
                                Process process = new Process();
                                process.StartInfo = startInfo;
                                process.Start();
                                processes.Add(process);

                                //if client  is online, we move on to the next event. If not, we wait until the client is online
                                IClientFacade cf = (IClientFacade)pms.getClientFacadeList()[username];
                                while (cf == null)
                                {
                                    Thread.Sleep(500);
                                    cf = (IClientFacade)pms.getClientFacadeList()[username];
                                }


                            }
                        }

                        break;

                    case "readCalendar":
                        username = line.Split(' ')[1].Trim();
                        icf = (IClientFacade)pms.getClientFacadeList()[username];
                        clientCalendar = icf.ReadCalendar();
                        show("Calendar for " + username + ": " + string.Join(",", clientCalendar));
                        break;

                    case "reservation":
                        //reservation {GroupMeeting; user1, user2; 13, 25 }
                        string data = line.Split('{')[1].Split('}')[0].Trim();
                        string desc = data.Split(';')[0].Trim();
                        string initiator = data.Split(';')[1].Split(',')[0].Trim();
                        List<string> usersList = data.Split(';')[1].Trim().Split(',').Select(s => s.Trim()).ToList<string>();
                        List<int> slotsList = data.Split(';')[2].Trim().Split(',').Select(s => s.Trim()).ToList<string>().ConvertAll<int>(Convert.ToInt32);

                        ReservationRequest rr = new ReservationRequest();
                        rr.Description = desc;
                        rr.Users = usersList;
                        rr.Slots = slotsList;
                        icf = (IClientFacade)pms.getClientFacadeList()[initiator];
                        while (icf == null)
                        {
                            show("Trying to reconnect to initiator: " + initiator);
                            icf = (IClientFacade)pms.getClientFacadeList()[initiator];
                            Thread.Sleep(100);
                        }

                        try
                        {
                            icf.CreateReservation(rr);
                        }
                        catch (Exception e)
                        {
                            show("Error on create reservation: " + e.Message);
                        }

                        Thread.Sleep(1000);
                        break;

                    case "wait":
                        int seconds = Convert.ToInt32(line.Split(' ')[1].Trim());
                        show("Sleeping for " + seconds + " second(s)");
                        Thread.Sleep(seconds * 1000);
                        break;

                    case "shutdown":
                        show("Shutting down all processes in 2 seconds");
                        Thread.Sleep(2000);


                        //killing all processes
                        foreach (Process p in processes)
                        {
                            
                            string userName = p.StartInfo.Arguments.Split(' ')[0];
                            p.Kill();
                            removeNodeFromTreeView(userName, null);
                            show("Killed " + p.StartInfo.Arguments);

                        }

                        //cleaning up
                        pms.getClientFacadeList().Clear();
                        pms.getServerFacadeList().Clear();
                        pms.getServersList().Clear();
                        pms.getClientsList().Clear();
                        processes.Clear();

                        //unregistering channels
                        UnregisterChannels();

                        show(" ");
                        show("----------------------SHUTDOWN COMPLETE--------------------");
                        show(" ");
                        break;
                }

            }

            sr.Close();
        }

        /*required for the shutdown command*/
        public void UnregisterChannels()
        {
            IChannel[] registeredChannels = ChannelServices.RegisteredChannels;
            string reg_channels = "";
            for (int i = 0; i < registeredChannels.Length; i++)
            {

                if (!registeredChannels[i].ChannelName.Equals("PuppetMasterChannel"))
                {
                    ChannelServices.UnregisterChannel(registeredChannels[i]);
                    reg_channels = reg_channels + registeredChannels[i].ChannelName + " ";
                }

            }
            show("Unregistered channels: "+reg_channels);
        }

        private void removeNodeFromTreeView(object sender, EventArgs ea)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, EventArgs>(removeNodeFromTreeView), sender, ea);
                return;
            }
            string username = (string)sender;
            //Clears all nodes.
            treeView1.Nodes.Find(username, true)[0].Remove();
        }


        private void changeIconToDisconnected(object sender, EventArgs ea)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, EventArgs>(changeIconToDisconnected), sender, ea);
                return;
            }

            string username = (string)sender;
            treeView1.Nodes.Find(username, true)[0].ImageIndex = 1;
            treeView1.Nodes.Find(username, true)[0].SelectedImageIndex = 1;
        }

        private void changeIconToConnected(object sender, EventArgs ea)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, EventArgs>(changeIconToConnected), sender, ea);
                return;
            }

            string username = (string)sender;
            treeView1.Nodes.Find(username, true)[0].ImageIndex = 0;
            treeView1.Nodes.Find(username, true)[0].SelectedImageIndex = 0;
        }

        private void consoleBox_TextChanged(object sender, EventArgs e)
        {

        }




    }
}
