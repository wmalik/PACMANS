using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
           /* string username = args[0];
            int port = Convert.ToInt32(args[1]);
            string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).CodeBase) + "\\";
            Console.WriteLine(username + " " + port);


            IServer server = new Server(username, port, path, "conf/Server.xml");
            Thread thread = new Thread(server.Init);
            thread.Start();
            */
            
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
