using System;
using System.IO;
using System.Threading.Tasks;

namespace HwGarage.Core.Http.Middleware
{
    public class ErrorHandlingMiddleware : BaseMiddleware
    {
        private readonly string _staticRoot;

        public ErrorHandlingMiddleware(BaseMiddleware? next, string staticRoot) : base(next)
        {
            _staticRoot = staticRoot;
        }

        public override async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await base.InvokeAsync(context);
            }
            catch (HttpNotFoundException)
            {
                await WriteErrorPageAsync(context, 404, "404.html");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {DateTime.Now}: {ex.Message}\n{ex.StackTrace}");
                Console.ResetColor();

                await WriteErrorPageAsync(context, 500, "500.html");
            }
        }

        private async Task WriteErrorPageAsync(HttpContext context, int statusCode, string fileName)
        {
            context.Response.StatusCode = statusCode;

            var errorDir = Path.Combine(_staticRoot, "errors");
            var filePath = Path.Combine(errorDir, fileName);

            if (File.Exists(filePath))
            {
                await context.SendFileAsync(filePath, "text/html; charset=utf-8");
            }
            else
            {
                var shortHtml = $@"<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <title>Ошибка {statusCode}</title>
    <link rel=""stylesheet"" href=""/css/style.css"">
</head>
<body class=""error-page"">
<div class=""card"">
    <h1>{statusCode}</h1>
    <p>Произошла ошибка.</p>
    <p><a href=""/"">Вернуться на главную</a></p>
</div>
</body>
</html>";

                await context.WriteAsync(shortHtml);
            }
        }
    }
}
