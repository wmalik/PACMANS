using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Beans;


namespace Server.Services
{

    /* SERVICES EXPOSED TO THE SERVERS */

    interface IConsistencyService
    {

        bool WriteClientMetadata(ClientMetadata clientInfo);

        ClientMetadata ReadClientMetadata(string username);

        bool WriteSequenceNumber(int seqNum);

        bool UnregisterUser(string username);

    }
}
