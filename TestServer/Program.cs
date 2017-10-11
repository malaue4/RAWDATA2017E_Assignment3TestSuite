using System;
using System.Collections.Generic;
using System.IO;
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

        private String[] methodNames = { "read", "create", "update", "delete","echo" };
        private String pathPrefix = "/api/";
        

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

            var strm = client.GetStream();
            //strm.ReadTimeout = 250;
            byte[] resp = new byte[2048];
            using (var memStream = new MemoryStream())
            {
                int bytesread = 0;
                do
                {
                    bytesread = strm.Read(resp, 0, resp.Length);
                    memStream.Write(resp, 0, bytesread);

                } while (bytesread == 2048);

                var responseData = Encoding.UTF8.GetString(memStream.ToArray());
                Console.WriteLine(responseData.ToString());
                RequestObject obj = new RequestObject();
                obj = JsonConvert.DeserializeObject<RequestObject>(responseData);
                String response;
                response = CheckValidity(obj);
                Console.WriteLine(response);
                //Console.WriteLine("Method: "+obj.method+" - Path: "+obj.path+" - Date: "+obj.date+" - Body: "+obj.body);
            }
        }

        private String CheckValidity(RequestObject _obj)
        {

            String status=" ";
            int statusCode = 1;
                if(_obj.method == null)
            {
                status += "missing method,";
                statusCode = 4;
            } else if (!Array.Exists(methodNames, delegate(string s) { return s.Equals(_obj.method); }))
                {
                status += "illegal method,";
                statusCode = 4;
                }

                if(_obj.path == null)
            {
                status += "missing path,";
                statusCode = 4;
            } else if (!_obj.path.StartsWith(pathPrefix))
            {
                status += "illegal path,";
                statusCode = 4;
            }
            if (_obj.date == null)
            {
                status += "missing path,";
                statusCode = 4;
            }
            else 
            {
                try
                {
                    int.Parse(_obj.date);
                }
                catch (Exception)
                {

                    status += "illegal date,";
                    statusCode = 4;

                }
            }

            if(_obj.body == null)
            {

                status += "missing body,";
                statusCode = 4;
            }
            status = statusCode + status;
            return status;

        }
        
            
        
    }
    class RequestObject {

        [JsonProperty("method")]
        public String method;

        [JsonProperty("path")]
        public String path;

        [JsonProperty("date")]
        public String date;

        [JsonProperty("body")]
        public String body;

        }
}
