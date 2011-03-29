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

            Thread.Sleep(1000);


            IServer client2 = new Server("conf/Server2.xml");
            Thread t2 = new Thread(client2.Init);
            t2.Start();

            Thread.Sleep(1000);

            IServer client3 = new Server("conf/Server3.xml");
            Thread t3 = new Thread(client3.Init);
            t3.Start();

            Thread.Sleep(1000);
        }
    }
}
