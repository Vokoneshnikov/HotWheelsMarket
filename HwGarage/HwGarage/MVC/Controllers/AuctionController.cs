using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using HwGarage.MVC.Services;
using HwGarage.MVC.Validation.Auction;

namespace HwGarage.MVC.Controllers
{
    public class AuctionController : BaseController
    {
        private readonly DbContext _db;
        private readonly AuctionService _auctionService;

        public AuctionController(ViewRenderer renderer, DbContext db)
            : base(renderer)
        {
            _db = db;
            _auctionService = new AuctionService(db);
        }

        public async Task Index(HttpContext context)
        {
            await _auctionService.FinalizeExpiredAuctionsAsync();

            string search = context.Request.QueryString["q"] ?? "";
            var auctions = await _db.Auctions
                .Where("status", "active")
                .ToListAsync();

            var auctionItems = new List<Dictionary<string, object>>();

            foreach (var auction in auctions)
            {
                var car = await _db.Cars.FindAsync(auction.Car_Id);
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

                decimal currentBid = auction.Current_Bid > 0
                    ? auction.Current_Bid
                    : auction.Start_Price;

                auctionItems.Add(new Dictionary<string, object>
                {
                    ["auctionId"]   = auction.Id,
                    ["name"]        = car.Name,
                    ["description"] = car.Description ?? "",
                    ["photoBlock"]  = photoBlock,
                    ["currentBid"]  = currentBid,
                    ["bidStep"]     = auction.Bid_Step,
                    ["endsAt"]      = auction.Ends_At.ToString("g")
                });
            }

            var model = new Dictionary<string, object>
            {
                ["search"] = search,
                ["auctions"] = auctionItems,
                ["auctionsEmptyMessage"] = auctionItems.Count == 0
                    ? "No active auctions found."
                    : string.Empty
            };

            await RenderView(context, "auction/index.html", model);
        }

        public async Task Create(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var model = await BuildCreateModelAsync(
                user,
                errorMessage: "",
                startPrice: "",
                bidStep: "",
                endsAt: ""
            );

            await RenderView(context, "auction/create.html", model);
        }

        public async Task CreatePost(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var form = await context.ReadFormAsync();

            string carIdRaw      = form["car_id"];
            string startPriceRaw = form["start_price"];
            string bidStepRaw    = form["bid_step"];
            string endsAtRaw     = form["ends_at"];

            var validationResult = AuctionCreateValidator.Validate(
                carIdRaw,
                startPriceRaw,
                bidStepRaw,
                endsAtRaw,
                out int carId,
                out decimal startPrice,
                out decimal bidStep,
                out DateTime endsAt);

            if (!validationResult.IsValid)
            {
                await RenderCreateWithError(context, user,
                    validationResult.ErrorMessage!,
                    startPriceRaw, bidStepRaw, endsAtRaw);
                return;
            }

            var serviceResult = await _auctionService.CreateAuctionAsync(
                user,
                carId,
                startPrice,
                bidStep,
                endsAt);

            if (!serviceResult.Success)
            {
                await RenderCreateWithError(context, user,
                    serviceResult.ErrorMessage ?? "Ошибка при создании аукциона.",
                    startPriceRaw, bidStepRaw, endsAtRaw);
                return;
            }

            context.Redirect("/auction");
        }

        private async Task<Dictionary<string, object>> BuildCreateModelAsync(
            User user,
            string errorMessage,
            string startPrice,
            string bidStep,
            string endsAt)
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
                ["startPrice"] = WebUtility.HtmlEncode(startPrice ?? ""),
                ["bidStep"]    = WebUtility.HtmlEncode(bidStep ?? ""),
                ["endsAt"]     = WebUtility.HtmlEncode(endsAt ?? "")
            };

            return model;
        }

        private async Task RenderCreateWithError(
            HttpContext context,
            User user,
            string errorMessage,
            string startPrice,
            string bidStep,
            string endsAt)
        {
            var model = await BuildCreateModelAsync(user, errorMessage, startPrice, bidStep, endsAt);
            await RenderView(context, "auction/create.html", model);
        }

        public async Task View(HttpContext context)
        {
            await _auctionService.FinalizeExpiredAuctionsAsync();

            var user = context.User as User;

            string idRaw = context.Request.QueryString["id"];
            if (!int.TryParse(idRaw, out int auctionId))
            {
                await context.WriteAsync("Invalid auction id");
                return;
            }

            var auction = await _db.Auctions.FindAsync(auctionId);
            if (auction == null)
            {
                await context.WriteAsync("Auction not found");
                return;
            }

            var car = await _db.Cars.FindAsync(auction.Car_Id);
            if (car == null)
            {
                await context.WriteAsync("Car not found");
                return;
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

            decimal currentBid = auction.Current_Bid > 0 ? auction.Current_Bid : auction.Start_Price;

            var bids = await _db.Bids
                .Where("auction_id", auction.Id)
                .ToListAsync();

            var bidItems = new List<Dictionary<string, object>>();
            foreach (var bid in bids)
            {
                var bidder = await _db.Users.FindAsync(bid.Bidder_Id);
                string bidderName = bidder?.Username ?? "Unknown";

                bidItems.Add(new Dictionary<string, object>
                {
                    ["bidderName"] = bidderName,
                    ["amount"]     = bid.Amount,
                    ["createdAt"]  = bid.Created_At.ToString("g")
                });
            }

            var model = new Dictionary<string, object>
            {
                ["auctionId"]   = auction.Id,
                ["name"]        = car.Name,
                ["description"] = car.Description ?? "",
                ["photoBlock"]  = photoBlock,
                ["currentBid"]  = currentBid,
                ["bidStep"]     = auction.Bid_Step,
                ["endsAt"]      = auction.Ends_At.ToString("g"),
                ["status"]      = auction.Status,

                ["bids"] = bidItems,
                ["bidsEmptyMessage"] = bidItems.Count == 0
                    ? "No bids yet."
                    : string.Empty,

                ["isAuthenticated"] = (user != null ? "true" : "false")
            };

            await RenderView(context, "auction/view.html", model);
        }

        public async Task BidPost(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var form = await context.ReadFormAsync();

            var validationResult = AuctionBidValidator.Validate(
                form["auction_id"],
                form["amount"],
                out int auctionId,
                out decimal amount);

            if (!validationResult.IsValid)
            {
                await context.WriteAsync(validationResult.ErrorMessage ?? "Invalid data");
                return;
            }

            var serviceResult = await _auctionService.PlaceBidAsync(user, auctionId, amount);
            if (!serviceResult.Success)
            {
                await context.WriteAsync(serviceResult.ErrorMessage ?? "Error");
                return;
            }

            context.Redirect($"/auction/view?id={auctionId}");
        }
    }
}
