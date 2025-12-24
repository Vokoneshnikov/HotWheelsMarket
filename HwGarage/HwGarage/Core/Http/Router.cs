using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HwGarage.Core.Http
{
    public class Router
    {
        private readonly Dictionary<string, Dictionary<string, Func<HttpContext, Task>>> _routes = new();
        
        private void Register(string method, string path, Func<HttpContext, Task> handler)
        {
            method = method.ToUpperInvariant();
            path = NormalizePath(path);

            if (!_routes.ContainsKey(path))
                _routes[path] = new Dictionary<string, Func<HttpContext, Task>>(StringComparer.OrdinalIgnoreCase);

            _routes[path][method] = handler;
        }
        
        public void Get(string path, Func<HttpContext, Task> handler) =>
            Register("GET", path, handler);
        
        public void Post(string path, Func<HttpContext, Task> handler) =>
            Register("POST", path, handler);
        
        public Func<HttpContext, Task>? Resolve(string method, string path)
        {
            method = method.ToUpperInvariant();
            path = NormalizePath(path);

            if (_routes.TryGetValue(path, out var methods) &&
                methods.TryGetValue(method, out var handler))
            {
                return handler;
            }

            return null;
        }
        
        public async Task RouteAsync(HttpContext context)
        {
            string method = context.Request.HttpMethod;
            string path = context.Request.Url!.AbsolutePath;

            var handler = Resolve(method, path);
            if (handler != null)
            {
                await handler(context);
            }
            else
            {
                throw new HttpNotFoundException($"{method} {path}");
            }
        }


        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";
            if (!path.StartsWith("/"))
                path = "/" + path;
            return path.TrimEnd('/').ToLowerInvariant();
        }
    }
}
