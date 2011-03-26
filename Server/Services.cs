using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Beans;


namespace Server.Services
{

    /* SERVICES EXPOSED TO THE CLIENTS */

    interface LookupService
    {

        void RegisterUser(string username, int port);

        void UnregisterUser(string username);

        ClientMetadata Lookup(string username);

    }

    interface SequenceNumberService
    {

        int nextSequenceNumber();

    }

    /* SERVICES EXPOSED TO THE SERVERS */

    interface ConsistencyService
    {

        bool WriteClientMetadata(ClientMetadata clientInfo);

        ClientMetadata ReadClientMetadata();

        bool WriteSequenceNumber(int seqNum);

        int ReadSequenceNumber();

    }
}
