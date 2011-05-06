using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Services;
using Common.Beans;
using Common.Util;
using Client.Services;
using System.Net.Sockets;
using System.Threading;

namespace Client
{

    public interface IClientMonitor
    {
        void StartMonitoring();

        void MonitorReservation(Reservation res);

        void RemoveReservation(int resID);

        void Disconnected(int resID, string userID);
    }

    public class ClientMonitor : IClientMonitor
    {

        private const int SERVER_QUERY_TIME = 500; //milliseconds

        private string _userName;
        private List<ServerMetadata> _servers;

        private IMessageDispatcher _msgDispatch;
        private Dictionary<int, Reservation> _monitoredReservations;

        ILookupService _activeServer;

        public ClientMonitor(IMessageDispatcher msgDispatch, string userName, List<ServerMetadata> servers)
        {
            _msgDispatch = msgDispatch;
            _monitoredReservations = new Dictionary<int, Reservation>();
            _userName = userName;
            _servers = servers;
            _activeServer = null;
        }


        void IClientMonitor.MonitorReservation(Reservation res)
        {
            //Lock here
            res.ClientStubs = LookupUsers(res);
            //Unlock here
            _monitoredReservations[res.ReservationID] = res;
        }

        void IClientMonitor.RemoveReservation(int resID)
        {
            Reservation res;
            if (_monitoredReservations.TryGetValue(resID, out res))
            {
                //Lock here
                res.ClientStubs.Clear();
                _monitoredReservations.Remove(resID);
                //Unock here
            }

            foreach (string userID in res.Participants)
            {
                bool remove = true;
                foreach(Reservation otherRes in _monitoredReservations.Values){
                    if (otherRes.Participants.Contains(userID))
                    {
                        remove = false;
                        break;
                    }
                }
                if (remove)
                {
                    _msgDispatch.ClearStub(userID);
                }
            }
        }

        void IClientMonitor.Disconnected(int resID, string userID)
        {
            Reservation res;
            if (_monitoredReservations.TryGetValue(resID, out res))
            {
                //Lock here
                res.ClientStubs.Remove(userID);
                //Unock here
            }
        }

        void IClientMonitor.StartMonitoring()
        {
            _activeServer = Helper.GetRandomServer(_servers);
            while (true)
            {
                //Monitor all monitored reservations
                foreach(Reservation res in _monitoredReservations.Values){
                    //Only check a reservation if some participant is not online
                    if (res.ClientStubs.Count < (res.Participants.Count - 1))
                    {
                        //copy client stubs dictionary (to avoid unecessary synchronization)
                        Dictionary<string, IBookingService> clientStubs = new Dictionary<string, IBookingService>(res.ClientStubs);
                        foreach (string userID in res.Participants)
                        {
                            //If we do not have the stub of a given participant, check for it
                            if (!userID.Equals(_userName) && !clientStubs.ContainsKey(userID))
                            {
                                Log.Debug(_userName, "Verifying if offline participant " + userID + " of reservation " + res.ReservationID + " came online.");
                                IBookingService client = LookupClientStub(userID);
                                if (client != null)
                                {
                                    Log.Show(_userName, "Participant " + userID + " of reservation " + res.ReservationID + " has just came online.");
                                    _msgDispatch.ClientConnected(userID, client);
                                    //Lock here
                                    res.ClientStubs[userID] = client;
                                    //Unlock here
                                }
                            }
                        }
                    }
                }

                Thread.Sleep(SERVER_QUERY_TIME);
            }
        }

        private Dictionary<string, IBookingService> LookupUsers(Reservation res)
        {
            Dictionary<string, IBookingService> onlineUsers = new Dictionary<string, IBookingService>();

            foreach (string userID in res.Participants)
            {
                if (!userID.Equals(_userName))
                {
                    IBookingService client = LookupClientStub(userID);

                    if (client != null)
                    {
                        _msgDispatch.ClientConnected(userID, client);
                        onlineUsers[userID] = client;
                    }
                }
            }

            return onlineUsers;
        }

        private IBookingService LookupClientStub(string userID)
        {
            ClientMetadata clientMd = null;

            bool contacted = false;

            while (!contacted)
            {
                try
                {
                    //TODO: Navaneeth: here
                    Log.Show(_userName, "#1");
                    clientMd = _activeServer.Lookup(userID);
                    Log.Show(_userName, "#2");
                    contacted = true;
                }
                catch (Exception)
                {
                    //server has failed
                    //update server reference
                    _activeServer = Helper.GetRandomServer(_servers);
                }
            }

            IBookingService client = null;

            if (clientMd != null)
            {

                String connectionString = "tcp://" + clientMd.IP_Addr + ":" + clientMd.Port + "/" + clientMd.Username + "/" + Common.Constants.BOOKING_SERVICE_NAME;

                //Log.Debug(_userName, "Looking up participant stub: " + connectionString);

                try
                {
                    client = (IBookingService)Activator.GetObject(
                                                                            typeof(ILookupService),
                                                                            connectionString);
                }
                catch (SocketException e)
                {
                    Log.Show(_userName, "ERROR: Could not connect to client: " + clientMd.Username + ". Exception: " + e);
                    //TODO: do something?
                }
            }

            return client;
        }
    }
}
