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
    public class AuctionsApiController : BaseController
    {
        private readonly DbContext _db;
        private readonly AuctionService _auctionService;

        public AuctionsApiController(ViewRenderer renderer, DbContext db)
            : base(renderer)
        {
            _db = db;
            _auctionService = new AuctionService(db);
        }

        // GET /api/auctions
        public async Task GetActiveAuctions(HttpContext context)
        {
            await _auctionService.FinalizeExpiredAuctionsAsync();

            var auctions = await _db.Auctions
                .Where("status", "active")
                .ToListAsync();

            var resultList = new System.Collections.Generic.List<object>();

            foreach (var auction in auctions)
            {
                var car = await _db.Cars.FindAsync(auction.Car_Id);
                if (car == null)
                    continue;

                var photo = await _db.CarPhotos
                    .Where("car_id", car.Id)
                    .FirstOrDefaultAsync();

                decimal currentBid = auction.Current_Bid > 0
                    ? auction.Current_Bid
                    : auction.Start_Price;

                resultList.Add(new
                {
                    id = auction.Id,
                    carName = car.Name,
                    description = car.Description ?? "",
                    currentBid,
                    bidStep = auction.Bid_Step,
                    endsAt = auction.Ends_At.ToString("g"),
                    status = auction.Status,
                    photoUrl = photo?.Photo_Url
                });
            }

            var json = JsonSerializer.Serialize(resultList);
            await context.WriteAsync(json, "application/json");
        }

        // POST /api/auctions
        // ожидает JSON: { carId, startPrice, bidStep, endsAt }  (endsAt в формате ISO или "yyyy-MM-ddTHH:mm")
        public async Task CreateAuction(HttpContext context)
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
                !root.TryGetProperty("startPrice", out var startPriceEl) ||
                !root.TryGetProperty("bidStep", out var bidStepEl) ||
                !root.TryGetProperty("endsAt", out var endsAtEl))
            {
                context.Response.StatusCode = 400;
                await context.WriteAsync(
                    "{\"success\":false,\"error\":\"Некорректные данные\"}",
                    "application/json");
                return;
            }

            if (!int.TryParse(carIdEl.ToString(), out var carId) ||
                !decimal.TryParse(startPriceEl.ToString(), out var startPrice) ||
                !decimal.TryParse(bidStepEl.ToString(), out var bidStep) ||
                !DateTime.TryParse(endsAtEl.ToString(), out var endsAt))
            {
                context.Response.StatusCode = 400;
                await context.WriteAsync(
                    "{\"success\":false,\"error\":\"Некорректный формат полей\"}",
                    "application/json");
                return;
            }

            var serviceResult = await _auctionService.CreateAuctionAsync(
                user,
                carId,
                startPrice,
                bidStep,
                endsAt
            );

            if (!serviceResult.Success)
            {
                context.Response.StatusCode = 400;
                await context.WriteAsync(
                    JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = serviceResult.ErrorMessage ?? "Ошибка при создании аукциона."
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
