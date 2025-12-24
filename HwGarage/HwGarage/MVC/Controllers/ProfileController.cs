using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using HwGarage.MVC.Validation.Profile;

namespace HwGarage.MVC.Controllers
{
    public class ProfileController : BaseController
    {
        private readonly DbContext _db;
        private readonly ProfileUpdateValidator _profileUpdateValidator = new();

        public ProfileController(ViewRenderer renderer, DbContext db) : base(renderer)
        {
            _db = db;
        }

        public async Task Index(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var cars = await _db.Cars
                .Where("owner_id", user.Id)
                .ToListAsync();

            var bids = await _db.Bids
                .Where("bidder_id", user.Id)
                .ToListAsync();

            var carItems = new List<Dictionary<string, object>>();
            foreach (var car in cars)
            {
                var photo = await _db.CarPhotos
                    .Where("car_id", car.Id)
                    .FirstOrDefaultAsync();

                string photoBlock;
                if (photo != null && !string.IsNullOrWhiteSpace(photo.Photo_Url))
                {
                    photoBlock =
                        $"<img src=\"{WebUtility.HtmlEncode(photo.Photo_Url)}\" " +
                        "alt=\"Car photo\" />";
                }
                else
                {
                    photoBlock = "Фото машинки";
                }

                carItems.Add(new Dictionary<string, object>
                {
                    ["name"]       = car.Name,
                    ["status"]     = car.Status,
                    ["description"]= car.Description ?? "",
                    ["photoBlock"] = photoBlock
                });
            }

            var bidItems = new List<Dictionary<string, object>>();
            foreach (var bid in bids)
            {
                bidItems.Add(new Dictionary<string, object>
                {
                    ["auctionId"] = bid.Auction_Id,
                    ["amount"]    = bid.Amount,
                    ["createdAt"] = bid.Created_At.ToString("g")
                });
            }

            var model = new Dictionary<string, object>
            {
                ["username"] = user.Username,
                ["firstName"] = user.FirstName ?? "",
                ["lastName"]  = user.LastName ?? "",
                ["email"]     = user.Email,
                ["balance"]   = user.Balance,

                ["cars"] = carItems,
                ["bids"] = bidItems,

                ["carsEmptyMessage"] = carItems.Count == 0
                    ? "No cars yet. Add your first one soon."
                    : string.Empty,

                ["bidsEmptyMessage"] = bidItems.Count == 0
                    ? "You haven't placed any bids yet."
                    : string.Empty
            };

            await RenderView(context, "profile/index.html", model);
        }

        public async Task Update(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var form = await context.ReadFormAsync();

            string firstName = form["firstName"]?.Trim() ?? "";
            string lastName  = form["lastName"]?.Trim() ?? "";
            string email     = form["email"]?.Trim() ?? "";

            var validationResult = _profileUpdateValidator.Validate(new ProfileUpdateInput
            {
                FirstName = firstName,
                LastName  = lastName,
                Email     = email
            });

            if (!validationResult.IsValid)
            {
                await context.WriteAsync(
                    validationResult.ErrorMessage ?? "Invalid profile data.",
                    "text/plain",
                    400);
                return;
            }

            user.FirstName = firstName;
            user.LastName  = lastName;
            user.Email     = email;

            await _db.Users.UpdateAsync(user.Id, user);

            context.Redirect("/profile");
        }
    }
}
