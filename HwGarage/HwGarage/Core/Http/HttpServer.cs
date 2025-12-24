using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HwGarage.Core.Http.Middleware;
using HwGarage.Core.Auth;
using HwGarage.Core.Orm;

namespace HwGarage.Core.Http
{
    public class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly BaseMiddleware _pipeline;

        public HttpServer(string prefix, Router router, SessionManager sessions, DbContext db, string staticPath)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);

            _pipeline = new ErrorHandlingMiddleware(
                new ValidationMiddleware(
                    new StaticFilesMiddleware(
                        new AuthMiddleware(
                            new RouterMiddleware(null, router),
                            sessions,
                            db
                        ),
                        staticPath
                    )
                ),
                staticPath
            );

        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine($"[SERVER] Listening on {_listener.Prefixes.First()}");

            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    var httpContext = new HttpContext(context);

                    await _pipeline.InvokeAsync(httpContext);

                    context.Response.OutputStream.Close();
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SERVER ERROR] {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _listener.Stop();
            Console.WriteLine("[SERVER] Stopped.");
        }
    }
}
