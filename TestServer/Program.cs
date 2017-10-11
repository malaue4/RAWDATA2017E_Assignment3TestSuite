using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace TestServer
{


    public class Response
    {
        public string Status { get; set; }
        public string Body { get; set; }
    }

    public class Category
    {
        [JsonProperty("cid")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }

        
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            new Program().StartServer();
        }

        private TcpListener _server;
        private List<Category> _categories;
        private bool isRunning;

        public Program()
        {
            _server = new TcpListener(IPAddress.Loopback, 5000);
            _server.Start();
            _categories = new List<Category>();
        }

        public void StartServer()
        {

            isRunning = true;

            while (isRunning)
            {
                if (_server.Pending())
                {
                    var client = _server.AcceptTcpClient();

                    Console.WriteLine("client accepted");
                    var thread = new Thread(HandleClient);

                    thread.Start(client);
                }
            }
        }

        public void StopServer()
        {
            isRunning = false;
            _server.Stop();
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
                networkStream.Close();
                client.Close();


            }
        }
    }
}
