using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Assignment3TestSuite;
using Newtonsoft.Json;

namespace TestServer
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            new Program();
        }

        private TcpListener _server;
        private List<Category> _categories;

        public Program()
        {
            _server = new TcpListener(IPAddress.Loopback, 5000);
            _server.Start();
            _categories = new List<Category>();

            while (true)
            {
                var client = _server.AcceptTcpClient();

                Console.WriteLine("client accepted");
                var thread = new Thread(HandleClient);
                
 //              _server.Stop();

                thread.Start(client);
            }

        }

        void HandleClient(object clientObject)
        {
            var client = clientObject as TcpClient;
            if (client == null) return;
            var networkStream = client.GetStream();

            while (networkStream.DataAvailable)
            {
                byte[] buffer = new byte[client.ReceiveBufferSize];
                var request = networkStream.Read(buffer, 0, buffer.Length);
                String requestStr = Encoding.UTF8.GetString(buffer);
                requestStr = requestStr.Trim('\0');
                Console.WriteLine(requestStr);
                var category = new Category();
                category = JsonConvert.DeserializeObject<Category>(requestStr);

                Console.WriteLine(category.Id+", "+category.Name);

                
            }


       
        }

    }
}
