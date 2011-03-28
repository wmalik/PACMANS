using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {

            IServer client1 = new Server("conf/Server1.xml");
            Thread t1 = new Thread(client1.Init);
            t1.Start();

        }
    }
}
