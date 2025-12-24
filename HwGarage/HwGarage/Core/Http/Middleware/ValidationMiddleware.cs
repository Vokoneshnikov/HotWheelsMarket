using System.Threading.Tasks;

namespace HwGarage.Core.Http.Middleware
{
    public class ValidationMiddleware : BaseMiddleware
    {
        public ValidationMiddleware(BaseMiddleware? next) : base(next) { }

        public override async Task InvokeAsync(HttpContext context)
        {
            // В ValidationMiddleware.InvokeAsync, перед проверками:
            context.Response.AddHeader("Access-Control-Allow-Origin", "http://localhost:5173");
            context.Response.AddHeader("Access-Control-Allow-Credentials", "true");
            context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            if (context.Request.HttpMethod == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                await context.WriteAsync("");
                return;
            }

            if (context.Request.ContentLength64 > 10_000_000)
            {
                context.Response.StatusCode = 413;
                await context.WriteAsync("<h1>413 Request Entity Too Large</h1>");
                return;
            }
            if (context.Request.HttpMethod != "GET" &&
                context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                await context.WriteAsync("<h1>405 Method Not Allowed</h1>");
                return;
            }

            await base.InvokeAsync(context);
        }
    }
}