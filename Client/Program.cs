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
    class Program
    {
        static void Main(string[] args)
        {
          
            string username = args[0];
            int port = Convert.ToInt32(args[1]);
            string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Client)).CodeBase)+"\\";
            Console.WriteLine(username+" "+port);
            

            IClient client = new Client(username, port, path, "conf/Client.xml");
            Thread thread = new Thread(client.Init);
            thread.Start();
            

            /*
            
            IClient client1 = new Client("conf/Client1.xml");
            Thread t1 = new Thread(client1.Init);
            t1.Start();

            Thread.Sleep(1000);


            IClient client2 = new Client("conf/Client2.xml");
            Thread t2 = new Thread(client2.Init);
            t2.Start();

            Thread.Sleep(1000);

            IClient client3 = new Client("conf/Client3.xml");
            Thread t3 = new Thread(client3.Init);
            t3.Start();

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
