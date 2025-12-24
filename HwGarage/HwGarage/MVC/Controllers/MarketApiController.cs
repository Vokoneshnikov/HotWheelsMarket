using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using HwGarage.MVC.Services;

namespace HwGarage.MVC.Controllers
{
    public class MarketApiController : BaseController
    {
        private readonly DbContext _db;
        private readonly MarketplaceService _marketplaceService;

        public MarketApiController(ViewRenderer renderer, DbContext db)
            : base(renderer)
        {
            _db = db;
            _marketplaceService = new MarketplaceService(db);
        }

        // GET /api/market
        public async Task GetListings(HttpContext context)
        {
            var listings = await _db.Listings
                .Where("status", "active")
                .ToListAsync();

            var resultList = new System.Collections.Generic.List<object>();

            foreach (var listing in listings)
            {
                var car = await _db.Cars.FindAsync(listing.Car_Id);
                if (car == null)
                    continue;

                var photo = await _db.CarPhotos
                    .Where("car_id", car.Id)
                    .FirstOrDefaultAsync();

                var seller = await _db.Users.FindAsync(listing.Seller_Id);
                string sellerName = seller?.Username ?? "Unknown";

                resultList.Add(new
                {
                    id = listing.Id,
                    carName = car.Name,
                    description = car.Description ?? "",
                    price = listing.Price,
                    sellerName,
                    photoUrl = photo?.Photo_Url
                });
            }

            var json = JsonSerializer.Serialize(resultList);
            await context.WriteAsync(json, "application/json");
        }

        // POST /api/market/add
        // ожидает JSON: { carId, price }
        public async Task AddListing(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Response.StatusCode = 401;
                await context.WriteAsync(
                    "{\"success\":false,\"error\":\"unauthorized\"}",
                    "application/json");
                return;
            }

            using var doc = await JsonDocument.ParseAsync(context.Request.InputStream);
            var root = doc.RootElement;

            if (!root.TryGetProperty("carId", out var carIdEl) ||
                !root.TryGetProperty("price", out var priceEl))
            {
                context.Response.StatusCode = 400;
                await context.WriteAsync(
                    "{\"success\":false,\"error\":\"Некорректные данные\"}",
                    "application/json");
                return;
            }

            if (!int.TryParse(carIdEl.ToString(), out var carId) ||
                !decimal.TryParse(priceEl.ToString(), out var price))
            {
                context.Response.StatusCode = 400;
                await context.WriteAsync(
                    "{\"success\":false,\"error\":\"Некорректный формат полей\"}",
                    "application/json");
                return;
            }

            var serviceResult = await _marketplaceService.CreateListingAsync(user, carId, price);

            if (!serviceResult.Success)
            {
                context.Response.StatusCode = 400;
                await context.WriteAsync(
                    JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = serviceResult.ErrorMessage ?? "Ошибка при создании объявления."
                    }),
                    "application/json");
                return;
            }

            await context.WriteAsync(
                "{\"success\":true}",
                "application/json");
        }

        // POST /api/market/buy
        // ожидает JSON: { listingId }
        public async Task Buy(HttpContext context)
        {
            var buyer = context.User as User;
            if (buyer == null)
            {
                context.Response.StatusCode = 401;
                await context.WriteAsync(
                    "{\"success\":false,\"error\":\"unauthorized\"}",
                    "application/json");
                return;
            }

            using var doc = await JsonDocument.ParseAsync(context.Request.InputStream);
            var root = doc.RootElement;

            if (!root.TryGetProperty("listingId", out var listingIdEl) ||
                !int.TryParse(listingIdEl.ToString(), out var listingId))
            {
                context.Response.StatusCode = 400;
                await context.WriteAsync(
                    "{\"success\":false,\"error\":\"Некорректные данные\"}",
                    "application/json");
                return;
            }

            var serviceResult = await _marketplaceService.BuyAsync(buyer, listingId);

            if (!serviceResult.Success)
            {
                context.Response.StatusCode = 400;
                await context.WriteAsync(
                    JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = serviceResult.ErrorMessage ?? "Ошибка покупки."
                    }),
                    "application/json");
                return;
            }

            await context.WriteAsync(
                "{\"success\":true}",
                "application/json");
        }
    }
}
