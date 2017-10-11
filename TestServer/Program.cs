using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
        private bool _isRunning;

        private readonly string[] _methodNames = { "read", "create", "update", "delete", "echo" };
        private readonly string _pathPrefix = "/api/categories";

        readonly string StatusOk = "1 Ok";
        readonly string StatusCreated = "2 Created";
        readonly string StatusUpdated = "3 Updated";
        readonly string StatusBadRequest = "4 Bad Request";
        readonly string StatusNotFound = "5 Not Found";
        readonly string StatusError = "6 Error";


        public Program()
        {
            _server = new TcpListener(IPAddress.Loopback, 5000);
            _categories = new List<Category>
            {
                new Category
                {
                    Id = 1,
                    Name = "Beverages"
                },
                new Category
                {
                    Id = 2,
                    Name = "Condiments"
                },
                new Category
                {
                    Id = 3,
                    Name = "Confections"
                }
            };
        }

        public void StartServer()
        {
            _server.Start();
            _isRunning = true;
            Console.WriteLine(_categories.ToJson());
            while (_isRunning)
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
            _isRunning = false;
            _server.Stop();
        }

        private void HandleClient(object clientObject)
        {
            try
            {
                using (var client = clientObject as TcpClient)
                {
                    Request request = client.ReadRequest();

                    Response response = CheckValidity(request);

                    if (response.Status.Contains("1"))
                    {
                        response = HandleRequest(request);
                    }

                    Console.WriteLine("-->");
                    Console.WriteLine(request.ToJson());
                    Console.WriteLine(response.ToJson());
                    Console.WriteLine("<--");

                    client.SendResponse(response.ToJson());

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong: " + e.Message);
            }
        }

        private Response HandleRequest(Request request)
        {
            Response response = new Response();
            string[] path = request.path.Split("/", StringSplitOptions.RemoveEmptyEntries);
            int pathId = -1;
            if (path[1] != "categories")
            {
                response.Status = StatusBadRequest;
                return response;

            }
            if (path.Length > 2)
            {
                try
                {
                    pathId = int.Parse(path[2]);
                }
                catch (Exception)
                {
                    response.Status = StatusBadRequest;
                    return response;
                }

            }

            switch (request.method)
            {
                case "read":
                    if (pathId > 0 && pathId <= _categories.Count)
                    {
                        response.Body = _categories[pathId - 1].ToJson();

                        response.Status = StatusOk;
                    }
                    else if (pathId > _categories.Count)
                    {
                        response.Status = StatusNotFound;
                    }
                    else
                    {
                        string allCats = "";

                        allCats = _categories.ToJson();

                        response.Body = allCats;

                        response.Status = StatusOk;
                    }

                    break;
                case "create":
                    if (pathId == -1)
                    {
                        Category cat = request.body.FromJson<Category>();
                        cat.Id = _categories.Count + 1;
                        _categories.Add(cat);
                        response.Body = cat.ToJson();
                        response.Status = StatusCreated;
                    }
                    else
                    {
                        response.Status = StatusBadRequest;
                    }
                    break;
                case "update":
                    if (pathId != -1)
                    {
                        if (pathId <= _categories.Count)
                        {
                            _categories[pathId - 1] = request.body.FromJson<Category>();
                            response.Status = StatusUpdated;
                        }
                        else
                        {
                            response.Status = StatusNotFound;
                        }
                    }
                    else
                    {
                        response.Status = StatusBadRequest;
                    }
                    break;
                case "delete":
                    if (pathId != -1)
                    {
                        if (pathId <= _categories.Count)
                        {
                            _categories.RemoveAt(pathId - 1);
                            response.Status = StatusOk;
                        }
                        else
                        {
                            response.Status = StatusNotFound;
                        }
                    }
                    else
                    {
                        response.Status = StatusBadRequest;
                    }
                    break;
                case "echo":
                    response.Body = request.body;
                    response.Status = StatusOk;
                    break;
            }
            
            return response;
        }

        private Response CheckValidity(Request request)
        {

            Response response = new Response();
            List<string> reasons = new List<string>();
            string statusCode = StatusOk;
            if (request.method == null)
            {
                reasons.Add("missing method");
                statusCode = StatusBadRequest;
            }
            else if (!Array.Exists(_methodNames, s => s.Equals(request.method)))
            {
                reasons.Add("illegal method");
                statusCode = StatusBadRequest;
            }


            if (request.method == "echo")
            {
                response.Body = request.body;

            }
            else
            {
                if (request.path == null)
                {
                    reasons.Add("missing resource");
                    statusCode = StatusBadRequest;
                }
                else if (!request.path.StartsWith(_pathPrefix))
                {
                    //status += "illegal resource,";
                    statusCode = StatusBadRequest;
                }
            }
            if (request.date == null)
            {
                reasons.Add("missing date");
                statusCode = StatusBadRequest;
            }
            else
            {
                try
                {
                    int.Parse(request.date);
                }
                catch (Exception)
                {

                    reasons.Add("illegal date");
                    statusCode = StatusBadRequest;

                }
            }
            if (request.method != "read" && request.method != "delete")
            {
                if (request.body == null)
                {

                    reasons.Add("missing body");
                    statusCode = StatusBadRequest;

                }
                else if (!request.body.StartsWith("{") && !request.body.EndsWith("}"))
                {
                    reasons.Add("illegal body");
                    statusCode = StatusBadRequest;

                }
            }


            if (reasons.Count == 0)
                response.Status = statusCode;
            else
                response.Status = statusCode + " " + string.Join(", ", reasons);
            return response;

        }

        public class Request
        {

            [JsonProperty("method")]
            public string method;

            [JsonProperty("path")]
            public string path;

            [JsonProperty("date")]
            public string date;

            [JsonProperty("body")]
            public string body;

        }

    }

    public static class Util
    {

        public static void SendResponse(this TcpClient client, string response)
        {
            var msg = Encoding.UTF8.GetBytes(response);
            client.GetStream().Write(msg, 0, msg.Length);
        }

        public static Program.Request ReadRequest(this TcpClient client)
        {
            var stream = client.GetStream();
            //strm.ReadTimeout = 250;
            byte[] resp = new byte[2048];
            using (var memStream = new MemoryStream())
            {
                int bytesread = 0;
                do
                {
                    bytesread = stream.Read(resp, 0, resp.Length);
                    memStream.Write(resp, 0, bytesread);

                } while (bytesread == 2048);

                var responseData = Encoding.UTF8.GetString(memStream.ToArray());
                return JsonConvert.DeserializeObject<Program.Request>(responseData);
            }
        }

        public static string ToJson(this object data)
        {
            return JsonConvert.SerializeObject(data,
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }

        public static T FromJson<T>(this string element)
        {
            return JsonConvert.DeserializeObject<T>(element);
        }

    }
}
