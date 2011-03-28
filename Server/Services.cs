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

        ClientMetadata ReadClientMetadata();

        bool WriteSequenceNumber(int seqNum);

        int ReadSequenceNumber();

    }
}
