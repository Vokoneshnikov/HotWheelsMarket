using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace HwGarage.Core.Http.Middleware
{
    public class StaticFilesMiddleware : BaseMiddleware
    {
        private readonly string _staticRoot;

        public StaticFilesMiddleware(BaseMiddleware? next, string staticRoot)
            : base(next)
        {
            _staticRoot = staticRoot ?? throw new ArgumentNullException(nameof(staticRoot));
        }

        public override async Task InvokeAsync(HttpContext context)
        {
            var url = context.Request.Url;
            var rawPath = url?.AbsolutePath ?? "/";

            if (!IsStaticPath(rawPath))
            {
                if (Next != null)
                    await Next.InvokeAsync(context);
                return;
            }

            var decodedPath = WebUtility.UrlDecode(rawPath);

            var relativePath = decodedPath.TrimStart('/');

            var localPath = Path.Combine(
                _staticRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)
            );

            if (!File.Exists(localPath))
            {
                context.Response.StatusCode = 404;
                await context.WriteAsync("Static file not found", "text/plain", 404);
                return;
            }

            var contentType = GetContentType(Path.GetExtension(localPath));
            await context.SendFileAsync(localPath, contentType);
        }

        private static bool IsStaticPath(string path)
        {
            return path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
                   // || path.StartsWith("/img", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/fonts", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetContentType(string ext)
        {
            ext = ext.ToLowerInvariant();

            return ext switch
            {
                ".html" => "text/html; charset=utf-8",
                ".htm"  => "text/html; charset=utf-8",
                ".css"  => "text/css; charset=utf-8",
                ".js"   => "application/javascript; charset=utf-8",
                ".png"  => "image/png",
                ".jpg"  => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif"  => "image/gif",
                ".svg"  => "image/svg+xml",
                ".ico"  => "image/x-icon",
                ".woff" => "font/woff",
                ".woff2"=> "font/woff2",
                _       => "application/octet-stream"
            };
        }
    }
}
