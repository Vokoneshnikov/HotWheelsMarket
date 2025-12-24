using System.Threading.Tasks;
using HwGarage.Core.Auth;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;

namespace HwGarage.Core.Http.Middleware
{
    public class UserContextMiddleware : BaseMiddleware
    {
        private readonly SessionManager _sessions;
        private readonly DbContext _db;

        public UserContextMiddleware(BaseMiddleware? next, SessionManager sessions, DbContext db)
            : base(next)
        {
            _sessions = sessions;
            _db = db;
        }

        public override async Task InvokeAsync(HttpContext context)
        {
            if (context.Items.TryGetValue("IsAuthenticated", out var authFlag) && (bool)authFlag)
            {
                var userId = (int?)context.Items["UserId"];
                if (userId.HasValue)
                {
                    var user = await _db.Users.FindAsync(userId.Value);
                    context.Items["CurrentUser"] = user;
                }
            }

            await base.InvokeAsync(context);
        }
    }
}