using HwGarage.Core.Auth;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HwGarage.Core.Http.Middleware
{
    public class AuthMiddleware : BaseMiddleware
    {
        private readonly SessionManager _sessions;
        private readonly DbContext _db;

        public AuthMiddleware(BaseMiddleware? next, SessionManager sessions, DbContext db)
            : base(next)
        {
            _sessions = sessions;
            _db = db;
        }

        public override async Task InvokeAsync(HttpContext context)
        {
            string? token = context.Request.Cookies["SESSION_ID"]?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                int? userId = _sessions.GetUserId(token);
                if (userId.HasValue)
                {
                    var user = await _db.Users
                        .Where("id", userId.Value)
                        .FirstOrDefaultAsync();

                    if (user != null)
                    {
                        var userRoles = await _db.UserRoles
                            .Where("user_id", user.Id)
                            .ToListAsync();

                        var roleNames = new List<string>();

                        foreach (var ur in userRoles)
                        {
                            var role = await _db.Roles.FindAsync(ur.Role_Id);
                            if (role != null)
                                roleNames.Add(role.Name);
                        }

                        user.Roles = roleNames;

                        context.User = user;
                    }
                }
            }

            await base.InvokeAsync(context);
        }
    }
}