using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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


    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            new Program().StartServer();
        }

        private TcpListener _server;
        //private List<Category> _categories;
        private DataBase _data;
        private bool _isRunning;

        private readonly string[] _methodNames = { "read", "create", "update", "delete", "echo" };
        private readonly string _pathPrefix = "/api/categories";

        private const string StatusOk = "1 Ok";
        private const string StatusCreated = "2 Created";
        private const string StatusUpdated = "3 Updated";
        private const string StatusBadRequest = "4 Bad Request";
        private const string StatusNotFound = "5 Not Found";
        private const string StatusError = "6 Error";


        public Program()
        {
            _server = new TcpListener(IPAddress.Loopback, 5000);
            _data = new DataBase();
            /*_categories = new List<Category>
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
            };*/
        }

        public void StartServer()
        {
            _server.Start();
            _isRunning = true;
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
                    
                    if (pathId != -1)
                    {
                        //if (pathId > 0 && pathId <= _categories.Count)
                        if (_data.HasCategory(pathId))
                        {
                            //response.Body = _categories[pathId - 1].ToJson();
                            response.Body = _data.GetCategory(pathId).ToJson();
                            response.Status = StatusOk;
                        }
                        else
                        {
                            response.Status = StatusNotFound;
                        }
                    }
                    else
                    {
                        //response.Body = _categories.ToJson();
                        response.Body = _data.GetCategories().ToJson();
                        response.Status = StatusOk;
                    }

                    break;
                case "create":
                    if (pathId == -1)
                    {
                        Category cat = request.body.FromJson<Category>();
                        //cat.Id = _categories.Count + 1;
                        //_categories.Add(cat);
                        cat = _data.CreateCategory(cat.Name);
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
                        //if (pathId <= _categories.Count)
                        if (_data.HasCategory(pathId))
                        {
                            //_categories[pathId - 1] = request.body.FromJson<Category>();
                            _data.UpdateCategory(request.body.FromJson<Category>());
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
                        //if (pathId <= _categories.Count)
                        if (_data.HasCategory(pathId))
                        {
                            //_categories.RemoveAt(pathId - 1);
                            _data.DeleteCategory(_data.GetCategory(pathId));
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

    }
}
