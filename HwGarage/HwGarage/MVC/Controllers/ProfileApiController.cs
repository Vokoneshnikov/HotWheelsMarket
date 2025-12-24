using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;

namespace HwGarage.MVC.Controllers
{
    public class ProfileApiController : BaseController
    {
        private readonly DbContext _db;

        public ProfileApiController(ViewRenderer renderer, DbContext db)
            : base(renderer)
        {
            _db = db;
        }

        // GET /api/profile
        public async Task GetProfile(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Response.StatusCode = 401;
                await context.WriteAsync(
                    "{\"error\":\"unauthorized\"}",
                    "application/json");
                return;
            }

            var cars = await _db.Cars
                .Where("owner_id", user.Id)
                .ToListAsync();

            var bids = await _db.Bids
                .Where("bidder_id", user.Id)
                .ToListAsync();

            // Подтягиваем фото для машин
            var photos = await _db.CarPhotos.ToListAsync();

            var responseObject = new
            {
                username = user.Username,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                balance = user.Balance,
                cars = cars.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    status = c.Status,
                    description = c.Description,
                    photoUrl = photos
                        .FirstOrDefault(p => p.Car_Id == c.Id)?.Photo_Url
                }),
                bids = bids.Select(b => new
                {
                    auctionId = b.Auction_Id,
                    amount = b.Amount,
                    createdAt = b.Created_At
                })
            };

            var json = JsonSerializer.Serialize(responseObject);
            await context.WriteAsync(json, "application/json");
        }
    }
}
