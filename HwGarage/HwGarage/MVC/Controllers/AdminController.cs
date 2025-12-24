using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HwGarage.Core.Auth;
using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using HwGarage.MVC.Services;
using HwGarage.MVC.Validation.Admin;

namespace HwGarage.MVC.Controllers
{
    public class AdminController : BaseController
    {
        private readonly DbContext _db;
        private readonly AdminService _adminService;

        public AdminController(ViewRenderer renderer, DbContext db)
            : base(renderer)
        {
            _db = db;
            _adminService = new AdminService(db);
        }

        public async Task Moderation(HttpContext context)
        {
            var user = context.User as User;
            if (user == null || !user.HasRole("moderator"))
            {
                context.Response.StatusCode = 403;
                await context.WriteAsync("<h1>403 Forbidden</h1><p>Access denied.</p>");
                return;
            }

            var pendingCars = await _db.Cars
                .Where("status", "pending")
                .ToListAsync();

            var items = new List<Dictionary<string, object>>();

            foreach (var car in pendingCars)
            {
                User? owner = null;
                if (car.Owner_Id != 0)
                {
                    owner = await _db.Users.FindAsync(car.Owner_Id);
                }

                var photo = await _db.CarPhotos
                    .Where("car_id", car.Id)
                    .FirstOrDefaultAsync();

                string photoBlock = "";
                if (photo != null && !string.IsNullOrWhiteSpace(photo.Photo_Url))
                {
                    photoBlock =
                        $"<img src=\"{WebUtility.HtmlEncode(photo.Photo_Url)}\" " +
                        "alt=\"Car photo\" />";
                }

                items.Add(new Dictionary<string, object>
                {
                    ["carId"]      = car.Id,
                    ["name"]       = car.Name,
                    ["description"]= car.Description ?? "",
                    ["ownerName"]  = owner?.Username ?? "Unknown",
                    ["createdAt"]  = car.Created_At.ToString("g"),
                    ["photoBlock"] = photoBlock
                });
            }

            var pageModel = new Dictionary<string, object>
            {
                ["pendingCars"]  = items,
                ["emptyMessage"] = items.Count == 0
                    ? "No pending cars for moderation."
                    : string.Empty
            };

            await RenderView(context, "admin/moderation.html", pageModel);
        }

        public async Task ModerationPost(HttpContext context)
        {
            var user = context.User as User;
            if (user == null || !user.HasRole("moderator"))
            {
                context.Response.StatusCode = 403;
                await context.WriteAsync("<h1>403 Forbidden</h1><p>Access denied.</p>");
                return;
            }

            var form = await context.ReadFormAsync();

            var validationResult = ModerationDecisionValidator.Validate(
                form["car_id"],
                form["decision"],
                out int carId,
                out string decision);

            if (!validationResult.IsValid)
            {
                await context.WriteAsync(validationResult.ErrorMessage ?? "Validation error");
                return;
            }

            var serviceResult = await _adminService.ApplyModerationDecisionAsync(carId, decision);
            if (!serviceResult.Success)
            {
                await context.WriteAsync(serviceResult.ErrorMessage ?? "Error");
                return;
            }

            context.Redirect("/admin/moderation");
        }
    }
}
