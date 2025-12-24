using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;

namespace HwGarage.MVC.Controllers
{
    public class MyCarsApiController : BaseController
    {
        private readonly DbContext _db;

        public MyCarsApiController(ViewRenderer renderer, DbContext db)
            : base(renderer)
        {
            _db = db;
        }

        // GET /api/my-cars/available
        public async Task GetAvailable(HttpContext context)
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

            // оставляем только доступные (как раньше в HTML-форме)
            var available = cars
                .Where(c => string.Equals(c.Status, "available", System.StringComparison.OrdinalIgnoreCase))
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name
                })
                .ToList();

            var json = JsonSerializer.Serialize(available);
            await context.WriteAsync(json, "application/json");
        }
    }
}