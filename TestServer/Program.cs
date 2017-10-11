﻿using System;
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
        private bool isRunning;

        private String[] methodNames = { "read", "create", "update", "delete","echo" };
        private String pathPrefix = "/api/categories";
        

        public Program()
        {
            _server = new TcpListener(IPAddress.Loopback, 5000);
            _server.Start();
            _categories = new List<Category>
            {
                new Category()
                {
                    Id = 1,
                    Name = "Beverages"
                },
                new Category()
                {
                    Id = 2,
                    Name = "Condiments"
                },
                new Category()
                {
                    Id = 3,
                    Name = "Confections"
                }
            };
        }

        public void StartServer()
        {

            isRunning = true;
            Console.WriteLine(_categories.ToJson());
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

            RequestObject request = client.ReadRequest();
            
            Response response = CheckValidity(request);

            if (response.Status.Contains("1"))
            {
                response = HandleRequest(request);
            }
            Console.WriteLine();
            Console.WriteLine(request.ToJson());
            Console.WriteLine(response.ToJson());
            Console.WriteLine();

            client.SendResponse(response.ToJson());


            /*
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
               // Console.WriteLine(responseData.ToString());
                RequestObject obj = new RequestObject();
                obj = JsonConvert.DeserializeObject<RequestObject>(responseData);
                Response responseObj;
                responseObj = CheckValidity(obj);
                String response = JsonConvert.SerializeObject(responseObj);
                Console.WriteLine(response);
                var msg = Encoding.UTF8.GetBytes(response);
                client.GetStream().Write(msg, 0, msg.Length);
                //Console.WriteLine("Method: "+obj.method+" - Path: "+obj.path+" - Date: "+obj.date+" - Body: "+obj.body);
            }*/
        }

        private Response HandleRequest(RequestObject _obj)
        {
            Response response = new Response();
            String status = " ";
            String statusCode = "1 Ok";
            String[] path = _obj.path.Split("/", StringSplitOptions.RemoveEmptyEntries);
            int pathId = -1;
            if (path.Length > 2)
            {
                try
                {
                    pathId = int.Parse(path[2]);
                }
                catch (Exception)
                {

                    statusCode = "4 Bad Request illegal resource";
                    status = statusCode + status;
                    response.Status = status;
                    return response;
                }
                
            }
            Console.WriteLine(path.Length);
            switch (_obj.method)
            {
                case "read":
                    if (pathId > 0 && pathId <= _categories.Count)
                    {
                        response.Body = _categories[pathId-1].ToJson();
                       
                        statusCode = "1 Ok";
                    }
                    else if(pathId > _categories.Count)
                    {

                        statusCode = "5 Not found";
                    }
                    else 
                    {
                        String allCats ="";
                       
                        allCats = _categories.ToJson();

                        response.Body = allCats;

                        statusCode = "1 Ok";
                    }
                   
                    break;
                case "create":
                    if (path.Length <= 3)
                    {
                        Category cat = _obj.body.FromJson<Category>();
                        Console.WriteLine(cat.Name);
                        cat.Id = _categories.Count + 1;
                        _categories.Add(cat);
                        response.Body = cat.ToJson();
                        statusCode = "2 Created";
                    } else
                    {
                        statusCode = "4 Bad Request";
                    }
                    break;
                case "update":

                    break;
                case "delete":

                    break;
                case "echo":

                    break;
            }

            status = statusCode + status;
            response.Status = status;
            return response;
        }

        private Response CheckValidity(RequestObject _obj)
        {

            Response response = new Response();
            String status = " ";
            String statusCode = "1 Ok";
                if(_obj.method == null)
            {
                status += "missing method,";
                statusCode = "4 Bad Request";
            } else if (!Array.Exists(methodNames, delegate(string s) { return s.Equals(_obj.method); }))
                {
                status += "illegal method,";
                statusCode = "4 Bad Request";
                }


            if (_obj.method == "echo")
            {
                response.Body = _obj.body;

            }
            else
            {
                if (_obj.path == null)
                {
                    status += "missing resource,";
                    statusCode = "4 Bad Request";
                }
                else if (!_obj.path.StartsWith(pathPrefix))
                {
                    status += "illegal resource,";
                    statusCode = "4 Bad Request";
                }
            }
            if (_obj.date == null)
            {
                status += "missing date,";
                statusCode = "4 Bad Request";
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
                    statusCode = "4 Bad Request";

                }
            }
            if (_obj.method != "read")
            {
                if (_obj.body == null)
                {

                    status += "missing body,";
                    statusCode = "4 Bad Request";

                }
                else if (!_obj.body.StartsWith("{") && !_obj.body.EndsWith("}"))
                {
                    status += "illegal body,";
                    statusCode = "4 Bad Request";

                }
            }

            status = statusCode + status;
            response.Status = status;
            return response;

        }
        
            
        
    
        public class RequestObject {

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

    public static class Util
    {

        public static void SendResponse(this TcpClient client, string response)
        {
            var msg = Encoding.UTF8.GetBytes(response);
            client.GetStream().Write(msg, 0, msg.Length);
        }

        public static Program.RequestObject ReadRequest(this TcpClient client)
        {
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
                return JsonConvert.DeserializeObject<Program.RequestObject>(responseData);
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
