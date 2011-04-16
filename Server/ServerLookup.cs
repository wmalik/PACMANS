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
        public bool _status;
        public ClientMetadata data;
        public ManualResetEvent waiter = new ManualResetEvent(false);

        // This is the call that the AsyncCallBack delegate will reference.
        public void OurRemoteAsyncCallBack(IAsyncResult ar)
        {
            RemoteAsyncDelegate del = (RemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
            _status = del.EndInvoke(ar);
            waiter.Set();
            return;
        }

        public void OurLookupAsyncCallBack(IAsyncResult ar)
        {
            RemoteLookupDelegate del = (RemoteLookupDelegate)((AsyncResult)ar).AsyncDelegate;
            data = del.EndInvoke(ar);
            waiter.Set();
            return;
        }
    }

    class ServerLookup: MarshalByRefObject, ILookupService
    {
        private string _username;
        ServerAction action;
        private List<ServerMetadata> _servers;
        int _sequenceNumber;

        public ServerLookup(string username, ServerAction action, List<ServerMetadata> _servers )
        {
            this._username = username;
            this.action = action;
            this._servers = _servers;
            _sequenceNumber = 0;
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
            }
            finally{
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

            callback returnedValueOnRegister2 = new callback();
            callback.RemoteAsyncDelegate RemoteDelforRegister2 = new callback.RemoteAsyncDelegate(() => server[1].WriteClientMetadata(client));
            AsyncCallback RemoteCallbackOnRegister2 = new AsyncCallback(returnedValueOnRegister2.OurRemoteAsyncCallBack);
            IAsyncResult RemArForRegister2 = RemoteDelforRegister2.BeginInvoke(RemoteCallbackOnRegister2, null);

            action.WriteClientMetadata(client); //First Self Register

            Log.Show(_username, "[REGISTER CLIENT] Waiting for atleast one Server to return");

            returnedValueOnRegister1.waiter.WaitOne();

            if (returnedValueOnRegister1._status == false)
            {
                Log.Show(_username, "[REGISTER CLIENT] One of the Servers failed to register");
                returnedValueOnRegister2.waiter.WaitOne();
                if (returnedValueOnRegister2._status == false)
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



        void ILookupService.UnregisterUser(string username)  //TODO: Yet to implement
        {
            Monitor.Enter(this);
            try
            {
                UnregisterUserFromOtherServers(username);
                Log.Show(_username, "Unregistered client: " + username);
            }
            finally
            {
                Monitor.Exit(this);
            }
        }


        private void UnregisterUserFromOtherServers(string username)
        {
            IConsistencyService[] server = new IConsistencyService[_servers.Count];
            for (int i = 0; i < _servers.Count; i++)
            {
                server[i] = getOtherServers(_servers[i]);
            }

            //Write  generated sequence number to ourself.
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


            callback returnedValueOnUnregister2 = new callback();
            callback.RemoteAsyncDelegate RemoteDelForUnregister2 = new callback.RemoteAsyncDelegate(() => server[1].UnregisterUser(username));
            AsyncCallback RemoteCallbackForUnregister2 = new AsyncCallback(returnedValueOnUnregister2.OurRemoteAsyncCallBack);
            IAsyncResult RemAr2ForUnregister = RemoteDelForUnregister2.BeginInvoke(RemoteCallbackForUnregister2, null);


            Log.Show(_username, "[UNREGISTER USER] Waiting for one Server to return");
            returnedValueOnUnregister1.waiter.WaitOne();

            if (returnedValueOnUnregister1._status == false)
            {
                Log.Show(_username, "[UNREGISTER USER] One of the servers failed to unregister!!");

                returnedValueOnUnregister2.waiter.WaitOne();

                if (returnedValueOnUnregister2._status == false)
                {
                    Log.Show(_username, "[UNREGISTER USER] Both the servers failed to unregister [WEIRD]");
                }
                else
                {
                    Log.Show(_username, "[UNREGISTER USER] One server successfully unregistered");

                }
            }
            else
            {
                Log.Show(_username, "[UNREGISTER USER] One server successfully unregistered");
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
            AsyncCallback RemoteCallbackOnLookup1 = new AsyncCallback(returnedValueOnLookup1.OurRemoteAsyncCallBack);
            IAsyncResult RemArForLookup1 = RemoteDelforLookup1.BeginInvoke(RemoteCallbackOnLookup1, null);

            callback returnedValueOnLookup2 = new callback();
            callback.RemoteLookupDelegate RemoteDelforLookup2 = new callback.RemoteLookupDelegate(() => server[1].ReadClientMetadata(username));
            AsyncCallback RemoteCallbackOnLookup2 = new AsyncCallback(returnedValueOnLookup2.OurRemoteAsyncCallBack);
            IAsyncResult RemArForLookup2 = RemoteDelforLookup2.BeginInvoke(RemoteCallbackOnLookup2, null);

            Log.Show(_username, "[READ METADATA] Waiting for one server to return");
            returnedValueOnLookup1.waiter.WaitOne();

            //Compare the received value
            bool dataEqual;
            ClientMetadata myData = action.ReadClientMetadata(username);
            dataEqual = CompareValues(myData, returnedValueOnLookup1.data);

            if (dataEqual)
            {
                Log.Show(_username, "[READ METADATA] First retreived value matches");
                return myData;
            }

            else
            {
                returnedValueOnLookup2.waiter.WaitOne();
                dataEqual = CompareValues(myData, returnedValueOnLookup2.data);
                if (dataEqual)
                {
                    Log.Show(_username, "[READ METADATA] Second retreived value matches");
                    return myData;
                }
                else
                {
                    dataEqual = CompareValues(returnedValueOnLookup1.data, returnedValueOnLookup2.data);
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
                WriteSequenceNumberOnOtherServers();  //to test client functionality, comment this line to generate sequence number from one server.
            }
            finally
            {
                Monitor.Exit(this);
            }
                
            Log.Show(_username, "[RETRIEVED SEQ NUMBER] Sequence number retrieved. Next sequence number is: " + (_sequenceNumber));
            return _sequenceNumber;
        }

        private void WriteSequenceNumberOnOtherServers()
        {
            _sequenceNumber++; //increment sequence number and then try to acquire.
            IConsistencyService[] server = new IConsistencyService[_servers.Count];
            for (int i = 0; i < _servers.Count; i++)
            {
                server[i] = getOtherServers(_servers[i]);
            }

            //Write  generated sequence number to ourself.
            bool status = action.WriteSequenceNumber(_sequenceNumber);
            if (status == false)
            {
                Log.Show(_username, "[SEQ NUMBER] Generated sequence number write to ourself failed: " + _sequenceNumber);
                _sequenceNumber++;
                WriteSequenceNumberOnOtherServers();
            }

            callback returnedValue1 = new callback();
            callback.RemoteAsyncDelegate RemoteDel1 = new callback.RemoteAsyncDelegate(() => server[0].WriteSequenceNumber(_sequenceNumber));
            AsyncCallback RemoteCallback1 = new AsyncCallback(returnedValue1.OurRemoteAsyncCallBack);
            IAsyncResult RemAr1 = RemoteDel1.BeginInvoke(RemoteCallback1, null);


            callback returnedValue2 = new callback();
            callback.RemoteAsyncDelegate RemoteDel2 = new callback.RemoteAsyncDelegate(() => server[1].WriteSequenceNumber(_sequenceNumber));
            AsyncCallback RemoteCallback2 = new AsyncCallback(returnedValue2.OurRemoteAsyncCallBack);
            IAsyncResult RemAr2 = RemoteDel2.BeginInvoke(RemoteCallback2, null);


          //  Log.Show(_username, "[SEQ NUMBER] Waiting for one Server to return");
            returnedValue1.waiter.WaitOne();

            if (returnedValue1._status == false)
            {
                Log.Show(_username, "[SEQ NUMBER] One of the Servers failed to set the sequence number: " + _sequenceNumber);
                returnedValue2.waiter.WaitOne();

                if (returnedValue2._status == false)
                {
                    Log.Show(_username, "[SEQ NUMBER] Both servers failed to set the sequence number: " + _sequenceNumber);
                    _sequenceNumber++;
                    WriteSequenceNumberOnOtherServers(); // try until you get a sequence number.
                }
                else
                {
                    Log.Show(_username, "[SEQ NUMBER] One server successfully set the sequence number: " + _sequenceNumber);
                }
            }
            else
            {
                Log.Show(_username, "[SEQ NUMBER] One of the servers successfully set the sequence number: "+ _sequenceNumber);
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
    }
}
