﻿using System;
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
                networkStream.Read(buffer, 0, buffer.Length);
                Console.WriteLine(Encoding.UTF8.GetString(buffer));
            }

        }

    }
}
