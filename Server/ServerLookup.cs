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

    class callback
    {
        public delegate bool RemoteAsyncDelegate();
        public delegate ClientMetadata RemoteLookupDelegate();
        public delegate Dictionary<string, ClientMetadata> RemoteUpdateDelegate();
        public bool _status;
        public ClientMetadata data = new ClientMetadata();
        public Dictionary<string, ClientMetadata> info;
        public ManualResetEvent waiter = new ManualResetEvent(false);

        // This is the call that the AsyncCallBack delegate will reference.
        public void OurRemoteAsyncCallBack(IAsyncResult ar)
        {
            Monitor.Enter(this);
            try
            {

                try
                {
                    RemoteAsyncDelegate del = (RemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
                    _status = del.EndInvoke(ar);
                    Console.WriteLine("\nSIGNALLED STATUS" + _status);
                    waiter.Set();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e.Message);
                    _status = false;
                    Console.WriteLine("\nFAILURE SIGNALLED STATUS" + _status);
                    waiter.Set();
                }
                return;
            }
            finally
            {
                Monitor.Exit(this);
            }

        }

        public void OurLookupAsyncCallBack(IAsyncResult ar)
        {
            Monitor.Enter(this);
            try
            {
                try
                {
                    RemoteLookupDelegate del = (RemoteLookupDelegate)((AsyncResult)ar).AsyncDelegate;
                    data = del.EndInvoke(ar);
                    Console.WriteLine("\nSIGNALLED STATUS");
                    waiter.Set();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e.Message);
                    data = null;
                    Console.WriteLine("\nFAILURE SIGNALLED STATUS");
                    waiter.Set();
                }

                return;
            }
            finally
            {
                Monitor.Exit(this);
            }
        }


        public void OurRemoteUpdateCallBack(IAsyncResult ar)
        {
            Monitor.Enter(this);
            try
            {

                try
                {
                    RemoteUpdateDelegate del = (RemoteUpdateDelegate)((AsyncResult)ar).AsyncDelegate;
                    info = new Dictionary<string,ClientMetadata> (del.EndInvoke(ar));
                    Console.WriteLine("\nSIGNALLED STATUS" + _status);
                    waiter.Set();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception" + e.Message);
                }
                return;
            }
            finally
            {
                Monitor.Exit(this);
            }

        }
    }

    class ServerLookup : MarshalByRefObject, ILookupService
    {
        private string _username;
        ServerAction action;
        private List<ServerMetadata> _servers;
        int _sequenceNumber;
        PuppetMasterService pms;

        public override object InitializeLifetimeService()
        {

            return null;

        }

        public ServerLookup(string username, ServerAction action, List<ServerMetadata> _servers)
        {
            this._username = username;
            this.action = action;
            this._servers = _servers;
            _sequenceNumber = 0;
        }

        public void setPMS(PuppetMasterService pms)
        {
            this.pms = pms;
        }

        void ILookupService.RegisterUser(string username, string ip, int port)
        {
            Monitor.Enter(this);
            try
            {
                ClientMetadata client = new ClientMetadata();
                client.IP_Addr = ip;
                client.Port = port;
                client.Username = username;
                RegisterInfoOnAllServer(client);
                Log.Show(_username, "Registered client " + username + ": " + ip + ":" + port);
                pms.show("[REGISTER USER]" + _username + "-Registered client " + username + ": " + ip + ":" + port);
            }
            finally
            {
                Monitor.Exit(this);
            }

        }


        private bool RegisterInfoOnAllServer(ClientMetadata client)
        {

            IConsistencyService[] server = new IConsistencyService[_servers.Count];
            for (int i = 0; i < _servers.Count; i++)
            {
                server[i] = getOtherServers(_servers[i]);
            }

            callback returnedValueOnRegister1 = new callback();
            callback.RemoteAsyncDelegate RemoteDelforRegister1 = new callback.RemoteAsyncDelegate(() => server[0].WriteClientMetadata(client));
            AsyncCallback RemoteCallbackOnRegister1 = new AsyncCallback(returnedValueOnRegister1.OurRemoteAsyncCallBack);
            IAsyncResult RemArForRegister1 = RemoteDelforRegister1.BeginInvoke(RemoteCallbackOnRegister1, null);

            callback.RemoteAsyncDelegate RemoteDelforRegister2 = new callback.RemoteAsyncDelegate(() => server[1].WriteClientMetadata(client));
            IAsyncResult RemArForRegister2 = RemoteDelforRegister2.BeginInvoke(RemoteCallbackOnRegister1, null);

            action.WriteClientMetadata(client); //First Self Register
            Log.Show(_username, "[REGISTER CLIENT] Registered on self!!");
            Log.Show(_username, "[REGISTER CLIENT] Waiting for atleast one Server to return");

            returnedValueOnRegister1.waiter.WaitOne();
            returnedValueOnRegister1.waiter.Reset();

            if (returnedValueOnRegister1._status == false)
            {
                Log.Show(_username, "[REGISTER CLIENT] One of the Servers failed to register");
                returnedValueOnRegister1.waiter.WaitOne();
                returnedValueOnRegister1.waiter.Reset();

                if (returnedValueOnRegister1._status == false)
                {
                    Log.Show(_username, "[REGISTER CLIENT] Both the Servers failed to register");
                    return false;
                }
                else
                {
                    Log.Show(_username, "[REGISTER CLIENT] One Server successfully registered");
                    return true;
                }
            }
            else
            {
                Log.Show(_username, "[REGISTER CLIENT] One Server successfully registered");
                return true;
            }
        }



        void ILookupService.UnregisterUser(string username)
        {
            Monitor.Enter(this);
            try
            {
                bool status = UnregisterUserFromOtherServers(username);
                if (status)
                {
                    Log.Show(_username, "[UNREGISTER USER] " + "Unregistered client: " + username);
                    pms.show("[UNREGISTER USER] " + _username + ": Unregistered client: " + username);
                }
                else
                {
                    Log.Show(_username, "[UNREGISTER USER] " + "No such entry exists: " + username);
                    pms.show("[UNREGISTER USER] " + _username + "No such entry exists: " + username);
                }
            }
            finally
            {
                Monitor.Exit(this);
            }
        }


        private bool UnregisterUserFromOtherServers(string username)
        {
            IConsistencyService[] server = new IConsistencyService[_servers.Count];
            for (int i = 0; i < _servers.Count; i++)
            {
                server[i] = getOtherServers(_servers[i]);
            }

           
            bool status = action.UnregisterUser(username);
            Log.Show(_username, "[UNREGISTER USER] Unregister user from self");
            if (status == false)
            {
                Log.Show(_username, "[UNREGISTER USER] Unregister user at self failed!!");
            }

            callback returnedValueOnUnregister1 = new callback();
            callback.RemoteAsyncDelegate RemoteDelForUnregister1 = new callback.RemoteAsyncDelegate(() => server[0].UnregisterUser(username));
            AsyncCallback RemoteCallbackForUnregister1 = new AsyncCallback(returnedValueOnUnregister1.OurRemoteAsyncCallBack);
            IAsyncResult RemAr1ForUnregister = RemoteDelForUnregister1.BeginInvoke(RemoteCallbackForUnregister1, null);

            callback.RemoteAsyncDelegate RemoteDelForUnregister2 = new callback.RemoteAsyncDelegate(() => server[1].UnregisterUser(username));
            IAsyncResult RemAr2ForUnregister = RemoteDelForUnregister2.BeginInvoke(RemoteCallbackForUnregister1, null);


            Log.Show(_username, "[UNREGISTER USER] Waiting for one Server to return");
            returnedValueOnUnregister1.waiter.WaitOne();
            returnedValueOnUnregister1.waiter.Reset();

            if (returnedValueOnUnregister1._status == false)
            {
                Log.Show(_username, "[UNREGISTER USER] One of the servers failed to unregister!!");

                returnedValueOnUnregister1.waiter.WaitOne();
                returnedValueOnUnregister1.waiter.Reset();

                if (returnedValueOnUnregister1._status == false)
                {
                    Log.Show(_username, "[UNREGISTER USER] Both the servers failed to unregister [WEIRD]");
                    return false;
                }
                else
                {
                    Log.Show(_username, "[UNREGISTER USER] One server successfully unregistered");
                    return true;

                }
            }
            else
            {
                Log.Show(_username, "[UNREGISTER USER] One server successfully unregistered");
                return true;
            }
        }


        ClientMetadata ILookupService.Lookup(string username)
        {
            Monitor.Enter(this);
            try
            {
                return (lookUpOnOtherServers(username));
            }
            finally
            {
                Monitor.Exit(this);
            }
        }


        private ClientMetadata lookUpOnOtherServers(string username)
        {
            IConsistencyService[] server = new IConsistencyService[_servers.Count];
            for (int i = 0; i < _servers.Count; i++)
            {
                server[i] = getOtherServers(_servers[i]);
            }

            callback returnedValueOnLookup1 = new callback();
            callback.RemoteLookupDelegate RemoteDelforLookup1 = new callback.RemoteLookupDelegate(() => server[0].ReadClientMetadata(username));
            AsyncCallback RemoteCallbackOnLookup1 = new AsyncCallback(returnedValueOnLookup1.OurLookupAsyncCallBack);
            IAsyncResult RemArForLookup1 = RemoteDelforLookup1.BeginInvoke(RemoteCallbackOnLookup1, null);

            callback.RemoteLookupDelegate RemoteDelforLookup2 = new callback.RemoteLookupDelegate(() => server[1].ReadClientMetadata(username));
            IAsyncResult RemArForLookup2 = RemoteDelforLookup2.BeginInvoke(RemoteCallbackOnLookup1, null);

            bool dataEqual;
            ClientMetadata myData = action.ReadClientMetadata(username);

            Log.Show(_username, "[READ METADATA] Waiting for one server to return");
            returnedValueOnLookup1.waiter.WaitOne();
            returnedValueOnLookup1.waiter.Reset();

            //Compare the received value
            ClientMetadata DataFromServer1 = returnedValueOnLookup1.data;
            dataEqual = CompareValues(myData, DataFromServer1);


            if (dataEqual)
            {
                Log.Show(_username, "[READ METADATA] First retreived value matches");
                return myData;
            }

            else
            {
                Log.Show(_username, "[READ METADATA] Waiting for second server to return");
                returnedValueOnLookup1.waiter.WaitOne();
                returnedValueOnLookup1.waiter.Reset();

                ClientMetadata DataFromServer2 = returnedValueOnLookup1.data;
                dataEqual = CompareValues(myData, DataFromServer2);
                if (dataEqual)
                {
                    Log.Show(_username, "[READ METADATA] Second retreived value matches");
                    return myData;
                }
                else
                {
                    dataEqual = CompareValues(DataFromServer1, DataFromServer1);
                    if (dataEqual)
                    {
                        Log.Show(_username, "[READ METADATA] I have an outdated value. Other two fetched values match");
                        action.WriteClientMetadata(returnedValueOnLookup1.data);
                        Log.Show(_username, "[READ METADATA] Updated my value to one of the received values");
                        return returnedValueOnLookup1.data;
                    }
                    else
                    {
                        Log.Show(_username, "[READ METADATA]ERROR - ERROR"); 
                        return null;
                    }
                }

            }
        }

        private bool CompareValues(ClientMetadata myValue, ClientMetadata receivedValue)
        {
            if ((myValue == null) || (receivedValue == null))
                return false;

            if ((myValue.Username == receivedValue.Username) && (myValue.IP_Addr == receivedValue.IP_Addr) && (myValue.Port == receivedValue.Port))
                return true;
            else
                return false;
        }



        int ILookupService.NextSequenceNumber()
        {
            //Lock the write sequence until it finishes
            Monitor.Enter(this);
            try
            {
                _sequenceNumber++; 
                WriteSequenceNumberOnOtherServers(); 
                Log.Show(_username, "[RETRIEVED SEQ NUMBER] Sequence number retrieved. Next sequence number is: " + (_sequenceNumber));
                //if (pms != null)
                Console.WriteLine("[PMS-DEBUG] "+pms.getClientsList().ToString());
                    pms.show(_username + " [RETRIEVED SEQ NUMBER] Sequence number retrieved. Next sequence number is: " + (_sequenceNumber));
                return _sequenceNumber;
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        private void WriteSequenceNumberOnOtherServers()
        {
            IConsistencyService[] server = new IConsistencyService[_servers.Count];
            for (int i = 0; i < _servers.Count; i++)
            {
                server[i] = getOtherServers(_servers[i]);
            }

            //Write  generated sequence number to ourself.
            TRYREWRITE:  bool status = action.WriteSequenceNumber(_sequenceNumber,_username);
            if (status == false)
            {
                Log.Show(_username, "[SEQ NUMBER] Generated sequence number write to ourself failed: " + _sequenceNumber);
                _sequenceNumber++;
                goto TRYREWRITE;
            }

            callback returnedValue1 = new callback();
            try
            {
                callback.RemoteAsyncDelegate RemoteDel1 = new callback.RemoteAsyncDelegate(() => server[0].WriteSequenceNumber(_sequenceNumber, _username));
                AsyncCallback RemoteCallback1 = new AsyncCallback(returnedValue1.OurRemoteAsyncCallBack);
                IAsyncResult RemAr1 = RemoteDel1.BeginInvoke(RemoteCallback1, null);


                callback.RemoteAsyncDelegate RemoteDel2 = new callback.RemoteAsyncDelegate(() => server[1].WriteSequenceNumber(_sequenceNumber, _username));
                IAsyncResult RemAr2 = RemoteDel2.BeginInvoke(RemoteCallback1, null);
            }
            catch (Exception e)
            {
                Log.Show(_username, "EXCEPTION: " + e.Message);
            }

            Log.Show(_username, "WAITING HERE FOR FIRST SERVER");
            returnedValue1.waiter.WaitOne();
            returnedValue1.waiter.Reset();

        
            if (returnedValue1._status == false)
            {
                Log.Show(_username, "[SEQ NUMBER] One of the Servers failed to set the sequence number: " + _sequenceNumber);
                Log.Show(_username, "WAITING HERE FOR SECOND SERVER ASSUMING FIRST RETURNED FALSE");
                returnedValue1.waiter.WaitOne();
                returnedValue1.waiter.Reset();

                Log.Show(_username, "STATUS" + returnedValue1._status);

                if (returnedValue1._status == false )
                {
                    Log.Show(_username, "[SEQ NUMBER] Both servers failed to set the sequence number: " + _sequenceNumber);
                    _sequenceNumber++;
                     goto TRYREWRITE; // try until you get a sequence number.
                }
                else
                {
                    Log.Show(_username, "[SEQ NUMBER] One server successfully set the sequence number: " + _sequenceNumber);
                }
            }
            else
            {
                Log.Show(_username, "[SEQ NUMBER] One of the servers successfully set the sequence number: " + _sequenceNumber);
 
            }
        }


        private IConsistencyService getOtherServers(ServerMetadata servers)
        {
            ServerMetadata chosenServer = servers;
            String connectionString = "tcp://" + chosenServer.IP_Addr + ":" + chosenServer.Port + "/" + servers.Username + "/" + Common.Constants.CONSISTENCY_SERVICE_NAME;
            Log.Show(_username, "Trying to find server: " + connectionString);

            IConsistencyService server = (IConsistencyService)Activator.GetObject(
                typeof(IConsistencyService),
                connectionString);

            return server;
        }

        public void UpdateInfo()
        {
            IConsistencyService[] server = new IConsistencyService[_servers.Count];
            for (int i = 0; i < _servers.Count; i++)
            {
                server[i] = getOtherServers(_servers[i]);
            }

            callback returnedValueOnUpdate = new callback();
            callback.RemoteUpdateDelegate RemoteDelforUpdate1 = new callback.RemoteUpdateDelegate(server[0].UpdateInfo);
            AsyncCallback RemoteCallbackOnUpdate = new AsyncCallback(returnedValueOnUpdate.OurRemoteUpdateCallBack);
            IAsyncResult RemArForLookup1 = RemoteDelforUpdate1.BeginInvoke(RemoteCallbackOnUpdate, null);

            callback.RemoteUpdateDelegate RemoteDelforUpdate2 = new callback.RemoteUpdateDelegate(server[1].UpdateInfo);
            IAsyncResult RemArForLookup2 = RemoteDelforUpdate2.BeginInvoke(RemoteCallbackOnUpdate, null);

            Log.Show(_username, "WAITING HERE FOR ONE SERVER");
            returnedValueOnUpdate.waiter.WaitOne();
            returnedValueOnUpdate.waiter.Reset();

            action.setinfo(returnedValueOnUpdate.info);

            return;
        }
    }
}
