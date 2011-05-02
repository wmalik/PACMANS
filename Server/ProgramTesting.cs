using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;

namespace Server
{
    class ProgramTesting
    {
        static void Main(string[] args)
        {
            string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(ProgramTesting)).CodeBase) + "\\";

            IServer client1 = new Server("central-1", 10000, path, "conf/Server.xml");
            Thread t1 = new Thread(client1.Init);
            t1.Start();

            Thread.Sleep(1000);


            IServer client2 = new Server("central-2", 10001, path, "conf/Server.xml");
            Thread t2 = new Thread(client2.Init);
            t2.Start();

            Thread.Sleep(1000);

            IServer client3 = new Server("central-3", 10002, path, "conf/Server.xml");
            Thread t3 = new Thread(client3.Init);
            t3.Start();

            Thread.Sleep(1000);            
        }
    }
}
