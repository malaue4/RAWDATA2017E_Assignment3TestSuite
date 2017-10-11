using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assignment3TestSuite;

namespace Assignment3
{
    class ServerProgram
    {
        private TcpListener _server;
        private List<Category> _categories;

        public ServerProgram()
        {
            _server = new TcpListener(IPAddress.Loopback, 5000);
            _server.Start();
            _categories = new List<Category>();

            while (true)
            {
                var client = _server.AcceptTcpClient();

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
