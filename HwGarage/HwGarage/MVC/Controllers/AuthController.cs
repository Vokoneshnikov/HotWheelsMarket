using HwGarage.Core.Auth;
using HwGarage.MVC;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using System.Threading.Tasks;
using HwGarage.Core.Http;
using System;
using System.Collections.Generic;
using System.Net;
using HwGarage.MVC.Validation.Auth;

namespace HwGarage.MVC.Controllers
{
    public class AuthController : BaseController
    {
        private readonly DbContext _db;
        private readonly SessionManager _sessions;

        private readonly LoginValidator _loginValidator = new();
        private readonly RegisterValidator _registerValidator = new();

        public AuthController(ViewRenderer renderer, DbContext db, SessionManager sessions)
            : base(renderer)
        {
            _db = db;
            _sessions = sessions;
        }

        public async Task Login(HttpContext context)
        {
            var model = new Dictionary<string, object>
            {
                ["error"] = "",
                ["username"] = ""
            };

            await RenderView(context, "auth/login.html", model);
        }

        public async Task LoginPost(HttpContext context)
        {
            var form = await context.ReadFormAsync();
            string username = form["username"]?.Trim() ?? "";
            string password = form["password"] ?? "";

            var validationResult = _loginValidator.Validate(new LoginInput
            {
                Username = username,
                Password = password
            });

            if (!validationResult.IsValid)
            {
                await RenderLoginWithError(context,
                    validationResult.ErrorMessage!,
                    username);
                return;
            }

            var user = await _db.Users
                .Where("username", username)
                .FirstOrDefaultAsync();

            if (user == null || !Hashing.VerifyPassword(password, user.PasswordHash))
            {
                await RenderLoginWithError(context,
                    "Неверное имя пользователя или пароль.",
                    username);
                return;
            }

            string token = _sessions.CreateSession(user.Id);
            context.SetCookie("SESSION_ID", token, httpOnly: true);

            context.Redirect("/profile");
        }

        private async Task RenderLoginWithError(HttpContext context, string errorMessage, string username)
        {
            var model = new Dictionary<string, object>
            {
                ["error"] = $"<div class=\"auth-card__error\">{WebUtility.HtmlEncode(errorMessage)}</div>",
                ["username"] = WebUtility.HtmlEncode(username ?? "")
            };

            await RenderView(context, "auth/login.html", model);
        }

        public async Task Register(HttpContext context)
        {
            var model = new Dictionary<string, object>
            {
                ["error"] = "",
                ["firstName"] = "",
                ["lastName"] = "",
                ["username"] = "",
                ["email"] = ""
            };

            await RenderView(context, "auth/register.html", model);
        }

        public async Task RegisterPost(HttpContext context)
        {
            var form = await context.ReadFormAsync();

            string firstName = form["firstName"]?.Trim() ?? "";
            string lastName  = form["lastName"]?.Trim() ?? "";
            string username  = form["username"]?.Trim() ?? "";
            string email     = form["email"]?.Trim() ?? "";
            string password  = form["password"] ?? "";

            var validationResult = _registerValidator.Validate(new RegisterInput
            {
                FirstName = firstName,
                LastName  = lastName,
                Username  = username,
                Email     = email,
                Password  = password
            });

            if (!validationResult.IsValid)
            {
                await RenderRegisterWithError(context,
                    validationResult.ErrorMessage!,
                    firstName, lastName, username, email);
                return;
            }

            var existingUserByUsername = await _db.Users
                .Where("username", username)
                .FirstOrDefaultAsync();

            if (existingUserByUsername != null)
            {
                await RenderRegisterWithError(context,
                    "Такое имя пользователя уже занято.",
                    firstName, lastName, username, email);
                return;
            }

            var existingUserByEmail = await _db.Users
                .Where("email", email)
                .FirstOrDefaultAsync();

            if (existingUserByEmail != null)
            {
                await RenderRegisterWithError(context,
                    "Пользователь с таким email уже существует.",
                    firstName, lastName, username, email);
                return;
            }

            string hash = Hashing.HashPassword(password);

            var newUser = new User
            {
                Username   = username,
                PasswordHash = hash,
                Email      = email,
                FirstName  = firstName,
                LastName   = lastName,
                CreatedAt  = DateTime.UtcNow
            };

            int newId = await _db.Users.InsertAsync(newUser);

            var userRole = await _db.Roles.Where("name", "user").FirstOrDefaultAsync();
            if (userRole != null)
            {
                await _db.UserRoles.InsertAsync(new UserRole
                {
                    User_Id = newId,
                    Role_Id = userRole.Id
                });
            }

            string token = _sessions.CreateSession(newId);
            context.SetCookie("SESSION_ID", token, httpOnly: true);

            context.Redirect("/profile");
        }

        public async Task Logout(HttpContext context)
        {
            string? token = context.Request.Cookies["SESSION_ID"]?.Value;
            if (!string.IsNullOrEmpty(token))
                _sessions.DestroySession(token);

            context.DeleteCookie("SESSION_ID");
            context.Redirect("/");
        }

        private async Task RenderRegisterWithError(
            HttpContext context,
            string errorMessage,
            string firstName,
            string lastName,
            string username,
            string email)
        {
            var model = new Dictionary<string, object>
            {
                ["error"] = $"<div class=\"auth-card__error\">{WebUtility.HtmlEncode(errorMessage)}</div>",
                ["firstName"] = WebUtility.HtmlEncode(firstName),
                ["lastName"]  = WebUtility.HtmlEncode(lastName),
                ["username"]  = WebUtility.HtmlEncode(username),
                ["email"]     = WebUtility.HtmlEncode(email)
            };

            await RenderView(context, "auth/register.html", model);
        }
    }
}
