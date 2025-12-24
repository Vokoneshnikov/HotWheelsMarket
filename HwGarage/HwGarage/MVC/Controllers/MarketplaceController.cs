using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using HwGarage.MVC.Services;
using HwGarage.MVC.Validation.Marketplace;

namespace HwGarage.MVC.Controllers
{
    public class MarketplaceController : BaseController
    {
        private readonly DbContext _db;
        private readonly MarketplaceService _marketplaceService;

        public MarketplaceController(ViewRenderer renderer, DbContext db)
            : base(renderer)
        {
            _db = db;
            _marketplaceService = new MarketplaceService(db);
        }
        
        public async Task Index(HttpContext context)
        {
            string search = context.Request.QueryString["q"] ?? "";

            var listings = await _db.Listings
                .Where("status", "active")
                .ToListAsync();

            var listingItems = new List<Dictionary<string, object>>();

            foreach (var listing in listings)
            {
                var car = await _db.Cars.FindAsync(listing.Car_Id);
                if (car == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(search) &&
                    car.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
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

                var seller = await _db.Users.FindAsync(listing.Seller_Id);
                string sellerName = seller?.Username ?? "Unknown";

                listingItems.Add(new Dictionary<string, object>
                {
                    ["listingId"]   = listing.Id,
                    ["name"]        = car.Name,
                    ["description"] = car.Description ?? "",
                    ["price"]       = listing.Price,
                    ["photoBlock"]  = photoBlock,
                    ["sellerName"]  = sellerName
                });
            }

            var model = new Dictionary<string, object>
            {
                ["search"] = search,
                ["listings"] = listingItems,
                ["listingsEmptyMessage"] = listingItems.Count == 0
                    ? "No active listings found."
                    : string.Empty
            };

            await RenderView(context, "market/index.html", model);
        }

        public async Task Add(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var model = await BuildAddModelAsync(
                user,
                errorMessage: "",
                price: ""
            );

            await RenderView(context, "market/add.html", model);
        }

        public async Task AddPost(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var form = await context.ReadFormAsync();

            string carIdRaw = form["car_id"];
            string priceRaw = form["price"];

            var validationResult = MarketplaceAddValidator.Validate(
                carIdRaw,
                priceRaw,
                out int carId,
                out decimal price);

            if (!validationResult.IsValid)
            {
                await RenderAddWithError(context, user,
                    validationResult.ErrorMessage!,
                    priceRaw);
                return;
            }

            var serviceResult = await _marketplaceService.CreateListingAsync(user, carId, price);
            if (!serviceResult.Success)
            {
                await RenderAddWithError(context, user,
                    serviceResult.ErrorMessage ?? "Ошибка при создании объявления.",
                    priceRaw);
                return;
            }

            context.Redirect("/market");
        }

        private async Task<Dictionary<string, object>> BuildAddModelAsync(
            User user,
            string errorMessage,
            string price)
        {
            var cars = await _db.Cars
                .Where("owner_id", user.Id)
                .ToListAsync();

            var availableCars = cars
                .Where(c => string.Equals(c.Status, "available", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var options = new StringBuilder();
            if (availableCars.Any())
            {
                foreach (var car in availableCars)
                {
                    options.Append(
                        $"<option value=\"{car.Id}\">{WebUtility.HtmlEncode(car.Name)}</option>");
                }
            }
            else
            {
                options.Append("<option disabled>No approved cars available</option>");
            }

            var model = new Dictionary<string, object>
            {
                ["carOptions"] = options.ToString(),
                ["error"] = string.IsNullOrEmpty(errorMessage)
                    ? ""
                    : $"<div class=\"form-error\">{WebUtility.HtmlEncode(errorMessage)}</div>",
                ["price"] = WebUtility.HtmlEncode(price ?? "")
            };

            return model;
        }

        private async Task RenderAddWithError(
            HttpContext context,
            User user,
            string errorMessage,
            string price)
        {
            var model = await BuildAddModelAsync(user, errorMessage, price);
            await RenderView(context, "market/add.html", model);
        }

        public async Task BuyPost(HttpContext context)
        {
            var buyer = context.User as User;
            if (buyer == null)
            {
                context.Redirect("/login");
                return;
            }

            var form = await context.ReadFormAsync();
            if (!int.TryParse(form["listing_id"], out int listingId))
            {
                await context.WriteAsync("Invalid listing id");
                return;
            }

            var serviceResult = await _marketplaceService.BuyAsync(buyer, listingId);
            if (!serviceResult.Success)
            {
                await context.WriteAsync(serviceResult.ErrorMessage ?? "Error");
                return;
            }

            context.Redirect("/profile");
        }
    }
}
