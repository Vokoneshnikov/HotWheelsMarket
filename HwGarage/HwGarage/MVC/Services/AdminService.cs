using System.Threading.Tasks;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;

namespace HwGarage.MVC.Services
{
    public class AdminService
    {
        private readonly DbContext _db;

        public AdminService(DbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult> ApplyModerationDecisionAsync(int carId, string decision)
        {
            var car = await _db.Cars.FindAsync(carId);
            if (car == null)
            {
                return ServiceResult.Fail("Car not found");
            }

            if (decision == "approve")
            {
                car.Status = "available";
            }
            else if (decision == "reject")
            {
                car.Status = "rejected";
            }
            else
            {
                return ServiceResult.Fail("Unknown decision");
            }

            await _db.Cars.UpdateAsync(car.Id, car);

            return ServiceResult.Ok();
        }
    }
}