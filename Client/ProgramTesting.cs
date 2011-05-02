using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection;


namespace Client
{
    class ProgramTesting
    {
        static void Main(string[] args)
        {
            string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(ProgramTesting)).CodeBase) + "\\";

            IClient client1 = new Client("test1", 4000, path, "conf/Client.xml");
            Thread t1 = new Thread(client1.Init);
            t1.Start();

            Thread.Sleep(1000);

            IClient client2 = new Client("test2", 4001, path, "conf/Client.xml");
            Thread t2 = new Thread(client2.Init);
            t2.Start();

            Thread.Sleep(1000);

            IClient client3 = new Client("test3", 4002, path, "conf/Client.xml");
            Thread t3 = new Thread(client3.Init);
            t3.Start();

            /*
            Thread.Sleep(1000);

            IClient client4 = new Client("conf/Client4.xml");
            Thread t4 = new Thread(client4.Init);
            t4.Start();

            Thread.Sleep(1000);

            IClient client5 = new Client("conf/Client5.xml");
            Thread t5 = new Thread(client5.Init);
            t5.Start();
             */
        }
    }
}
