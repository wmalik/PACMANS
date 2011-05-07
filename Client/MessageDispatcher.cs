using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Client.Services;
using Common.Util;
using Common.Beans;
using System.Net;
using System.Runtime.Remoting;
using PuppetMaster;

namespace Client
{

    public interface IMessageDispatcher
    {
        void SendMessage(MessageType type, int resID, string userID, params object[] params1);

        void ClientDisconnected(int resID, string userID);

        void ClientConnected(string userID, IBookingService client);

        void ClearMessages(List<string> participants, int resID);

        void ClearStub(string userID);

        void setPMSObject(PuppetMasterService pms);

        //Statistics
        int getMessageCount();
        int getInitResCount();
        int getBookSlotCount();
        int getPreCommitCount();
        int getDoCommitCount();
        

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
        private Dictionary<string, IAsyncResult> _previousCall;
        private int _messageCount = 0;
        private int _initResCount = 0;
        private int _bookSlotCount = 0;
        private int _preCommitCount = 0;
        private int _doCommitCount = 0;
        private PuppetMasterService _pms;


        

        public MessageDispatcher(string userName)
        {
            _userName = userName;
            _clients = new Dictionary<string, IBookingService>();
            _msgQueue = new Dictionary<string, List<Tuple<MessageType, int, object[]>>>();
            _previousCall = new Dictionary<string, IAsyncResult>();
        }

        public void setPMSObject(PuppetMasterService pms)
        {
            _pms = pms;
        }

        /*     STATISTICS FUNCTIONS    */
        public int getMessageCount()
        {
            return _messageCount;
        }

        public int getInitResCount()
        {
            return _initResCount;
        }

        public int getBookSlotCount()
        {
            return _bookSlotCount;
        }

        public int getPreCommitCount()
        {
            return _preCommitCount;
        }

        public int getDoCommitCount()
        {
            return _doCommitCount;
        }

        /*  STATISTICS FUNCTIONS END HERE   */
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
                    IAsyncResult result = null;

                    //Apparently there is a problem of sending two async messages very close
                    //to each other
                    //Wait until previous call was completed before sending next message
                    if (_previousCall.TryGetValue(userID, out result) && !result.IsCompleted)
                    {
                        Log.Debug(_userName, "Previous call was not completed, waiting 10ms before sending next message.");
                        result.AsyncWaitHandle.WaitOne(10);
                    }

                    switch (type)
                    {
                        case MessageType.INIT_RESERVATION:
                            InitReservationDelegate initRes = new InitReservationDelegate(stub.InitReservation);
                            result = initRes.BeginInvoke((ReservationRequest)msgParams[0], resID, (string)msgParams[1], (string)msgParams[2], (int)msgParams[3], null, null);
                            _initResCount++;
                            break;

                        case MessageType.BOOK_SLOT:
                            ParticipantDelegate bookSlot = new ParticipantDelegate(stub.BookSlot);
                            result = bookSlot.BeginInvoke(resID, (int)msgParams[0], null, null);
                            _bookSlotCount++;
                            break;

                        case MessageType.PRE_COMMIT:
                            ParticipantDelegate preCommit = new ParticipantDelegate(stub.PrepareCommit);
                            result = preCommit.BeginInvoke(resID, (int)msgParams[0], null, null);
                            _preCommitCount++;
                            break;

                        case MessageType.DO_COMMIT:
                            ParticipantDelegate doCommit = new ParticipantDelegate(stub.DoCommit);
                            result = doCommit.BeginInvoke(resID, (int)msgParams[0], null, null);
                            _doCommitCount++;
                            break;
                    }

                    //total counter for reservation messages between clients
                    _messageCount++;
                    

                    Log.Debug(_userName, "Sucessfully sent " + type + " message to participant " + userID + " of reservation " + resID);
                    _previousCall[userID] = result;
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
            _previousCall.Remove(userID);
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

        public void ClearMessages(List<string> participants, int resID)
        {
            foreach (string userID in participants)
            {
                if (!userID.Equals(_userName))
                {
                    List<Tuple<MessageType, int, object[]>> userQueue;
                    if (_msgQueue.TryGetValue(userID, out userQueue))
                    {
                        foreach(Tuple<MessageType, int, object[]> msg in new List<Tuple<MessageType, int, object[]>>(userQueue)){
                            if(msg.Item2 == resID){
                                userQueue.Remove(msg);
                            }
                        }
                    }
                }
            }
        }

        public void ClearStub(string userID)
        {
            //Get lock
            _clients.Remove(userID);
            _msgQueue.Remove(userID);
            _previousCall.Remove(userID);
            //Release lock
        }
    }
}
