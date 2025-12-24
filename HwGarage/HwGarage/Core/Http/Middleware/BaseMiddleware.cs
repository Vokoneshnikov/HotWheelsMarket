using System.Threading.Tasks;

namespace HwGarage.Core.Http.Middleware
{
    public abstract class BaseMiddleware
    {
        protected BaseMiddleware? Next { get; }

        protected BaseMiddleware(BaseMiddleware? next)
        {
            Next = next;
        }

        public virtual async Task InvokeAsync(HttpContext context)
        {
            if (Next != null)
                await Next.InvokeAsync(context);
        }
    }
}