using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Client.Services;
using Common.Util;
using Common.Beans;
using System.Net;
using System.Runtime.Remoting;

namespace Client
{

    public interface IMessageDispatcher
    {
        void SendMessage(MessageType type, int resID, string userID, params object[] params1);

        void ClientDisconnected(int resID, string userID);

        void ClientConnected(string userID, IBookingService client);

        void ClearStub(string userID);
    }

    delegate void InitReservationDelegate(ReservationRequest req, int resID, string initiatorID, string initiatorIP, int initiatorPort);

    delegate void ParticipantDelegate(int resID, int slotID);

    public enum MessageType
    {
        INIT_RESERVATION, BOOK_SLOT, PRE_COMMIT, DO_COMMIT
    }


    class MessageDispatcher : IMessageDispatcher
    {

        private string _userName;
        private Dictionary<string, IBookingService> _clients;
        private Dictionary<string, List<Tuple<MessageType, int, object[]>>> _msgQueue;

        public MessageDispatcher(string userName)
        {
            _userName = userName;
            _clients = new Dictionary<string, IBookingService>();
            _msgQueue = new Dictionary<string, List<Tuple<MessageType, int, object[]>>>();
        }

        public void SendMessage(MessageType type, int resID, string userID, params object[] msgParams)
        {
            SendMessage(true, type, resID, userID, msgParams);
        }

        private bool SendMessage(bool enqueue, MessageType type, int resID, string userID, params object[] msgParams)
        {
            bool success = true;
            IBookingService stub;
            if (_clients.TryGetValue(userID, out stub))
            {
                try
                {
                    IAsyncResult result;
                    switch (type)
                    {
                        case MessageType.INIT_RESERVATION:
                            InitReservationDelegate initRes = new InitReservationDelegate(stub.InitReservation);
                            result = initRes.BeginInvoke((ReservationRequest)msgParams[0], resID, (string)msgParams[1], (string)msgParams[2], (int)msgParams[3], null, null);
                            break;

                        case MessageType.BOOK_SLOT:
                            ParticipantDelegate bookSlot = new ParticipantDelegate(stub.BookSlot);
                            result = bookSlot.BeginInvoke(resID, (int)msgParams[0], null, null);
                            break;

                        case MessageType.PRE_COMMIT:
                            ParticipantDelegate preCommit = new ParticipantDelegate(stub.PrepareCommit);
                            result = preCommit.BeginInvoke(resID, (int)msgParams[0], null, null);
                            break;

                        case MessageType.DO_COMMIT:
                            ParticipantDelegate doCommit = new ParticipantDelegate(stub.DoCommit);
                            result = doCommit.BeginInvoke(resID, (int)msgParams[0], null, null);
                            break;
                    }
                    Log.Debug(_userName, "Sucessfully sent " + type + " message to participant " + userID + " of reservation " + resID);
                }
                catch (Exception)
                {
                    ClientDisconnected(resID, userID);
                    success = false;
                }
            }
            else
            {
                success = false;
            }

            if (!success && enqueue)
            {
                EnqueueMessage(type, resID, userID, msgParams);
            }

            return success;
        }

        public void EnqueueMessage(MessageType type, int resID, string userID, params object[] msgParams)
        {
            Log.Debug(_userName, "Client " + userID + " is not online. Enqueueing message " + type + "  of reservation " + resID);
            List<Tuple<MessageType, int, object[]>> userQueue;
            if (!_msgQueue.TryGetValue(userID, out userQueue))
            {
                userQueue = _msgQueue[userID] = new List<Tuple<MessageType, int, object[]>>();
            }

            userQueue.Add(new Tuple<MessageType, int, object[]>(type, resID, msgParams));
        }

        public void ClientDisconnected(int resID, string userID)
        {
            Log.Debug(_userName, "Client disconnected: " + userID);
            //Get lock
            _clients.Remove(userID);
            //Get lock
        }

        public void ClientConnected(string userID, IBookingService client)
        {
            Log.Debug(_userName, "Client connected: " + userID);
            //Get lock
            _clients[userID] = client;
            //Release lock

            List<Tuple<MessageType, int, object[]>> userQueue;
            if (_msgQueue.TryGetValue(userID, out userQueue))
            {
                Log.Debug(_userName, "Sending enqueued messages.");
                foreach (Tuple<MessageType, int, object[]> msg in new List<Tuple<MessageType, int, object[]>>(userQueue))
                {
                    if (SendMessage(false, msg.Item1, msg.Item2, userID, msg.Item3))
                    {
                        userQueue.Remove(msg);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public void ClearStub(string userID)
        {
            //Get lock
            _clients.Remove(userID);
            _msgQueue.Remove(userID);
            //Release lock
        }
    }
}
