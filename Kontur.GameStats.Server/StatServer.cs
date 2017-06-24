using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kontur.GameStats.Server.Routing;
using Kontur.GameStats.Server.API;
using System.Text;
using System.Collections.Generic;

namespace Kontur.GameStats.Server
{
    internal enum HttpMethodType { GET, PUT };
    internal class StatServer : IDisposable
    {
        private readonly HttpListener listener;
        private readonly RoutingTree routingTree;

        private readonly string logFilePath = @"Data\log.txt";
        private readonly bool isLogNotOKResponses = true;
        private readonly object syncRoot = new object();

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
        public StatServer()
        {
            listener    = new HttpListener();
            routingTree = new RoutingTree();
            InitializeRoutingTree();
        }
        public void InitializeRoutingTree()
        {
            routingTree.AddRule(
                "PUT/servers/?endpoint/info",
                param => new ServerController().PutInfo((string)param["?endpoint"],
                                                        (string)param["requestBody"]));
            routingTree.AddRule(
                "PUT/servers/?endpoint/matches/?timestamp",
                param => new MatchController().PutMatch((string)param["?endpoint"],
                                                        (string)param["requestBody"],
                                                        (DateTime)param["?timestamp"]));
            routingTree.AddRule(
                "GET/servers/info",
                param => new ServerController().GetInfo());
            routingTree.AddRule(
                "GET/servers/?endpoint/info",
                param => new ServerController().GetInfo((string)param["?endpoint"]));
            routingTree.AddRule(
                "GET/servers/?endpoint/stats",
                param => new ServerController().GetStats((string)param["?endpoint"]));
            routingTree.AddRule(
                "GET/servers/?endpoint/matches/?timestamp",
                param => new MatchController().GetMatch((string)param["?endpoint"],
                                                        (DateTime)param["?timestamp"]));
            routingTree.AddRule(
                "GET/players/?name/stats",
                param => new PlayerController().GetStats((string)param["?name"]));
            routingTree.AddRule(
                "GET/reports/recent-matches/?count",
                param => new MatchController().GetRecentMatches((int)param["?count"]));
            routingTree.AddRule(
                "GET/reports/best-players/?count",
                param => new PlayerController().GetBestPlayers((int)param["?count"]));
            routingTree.AddRule(
                "GET/reports/popular-servers/?count",
                param => new ServerController().GetPopularServers((int)param["?count"]));
        }
        public void Start(string prefix)
        {
            lock (listener)
            {
                if (!isRunning)
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listenerThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();
                    
                    isRunning = true;
                }
            }
        }

        public void Stop()
        {
            lock (listener)
            {
                if (!isRunning)
                    return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();
                
                isRunning = false;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }
        
        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception)
                {
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            string requestBody = String.Empty;

            try
            {
                // read body
                using (var receiveStream = listenerContext.Request.InputStream)
                using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    requestBody = readStream.ReadToEnd();
                }

                // parse url
                List<string> tokens = new List<string>
                {
                    listenerContext.Request.HttpMethod
                };
                tokens.AddRange(listenerContext.Request.RawUrl.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries));
                ApiResponse response = routingTree.Route(tokens, requestBody);

                // write log if bad result and necessary
                if (response.Status != HttpStatusCode.OK && isLogNotOKResponses)
                {
                    WriteError("DEBUG",
                               String.Format("StatusCode: {0}. Body: {1}",
                                             response.Status,
                                             response.Body),
                               listenerContext.Request.RawUrl,
                               requestBody);
                }

                // send response
                listenerContext.Response.StatusCode = (int)response.Status;
                using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
                {
                    writer.Write(response.Body);
                }
            }
            catch (Exception error)
            {
                // close connection
                listenerContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                listenerContext.Response.Close();
                // write log
                WriteError("FATAL", 
                            String.Format("Method: [{0}.{1}()]. Message: {2}",
                                            error.TargetSite.DeclaringType,
                                            error.TargetSite.Name,
                                            error.Message),
                            listenerContext.Request.RawUrl,
                            requestBody);
            }
        }
        private void WriteError(string errorType, string text, string request, string requestBody)
        {
            string fullText = string.Format(
                    "[{0}][{1:dd.MM.yyy HH:mm:ss}] {2}\r\n\t\t\t  Request: {3}\r\n\t\t\t  RequestBody: {4}\r\n",
                     errorType,                             
                     DateTime.Now,
                     text,
                     request,
                     requestBody
                     );
            lock (syncRoot)
            {
                File.AppendAllText(logFilePath, fullText);
            }
        }
    }
    
}