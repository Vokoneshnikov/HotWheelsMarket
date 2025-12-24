using System.Threading.Tasks;

namespace HwGarage.Core.Http.Middleware
{
    public class RouterMiddleware : BaseMiddleware
    {
        private readonly Router _router;

        public RouterMiddleware(BaseMiddleware? next, Router router) : base(next)
        {
            _router = router;
        }

        public override async Task InvokeAsync(HttpContext context)
        {
            await _router.RouteAsync(context);
        }
    }
}