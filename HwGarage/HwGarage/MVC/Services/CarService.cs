using System;
using System.Threading.Tasks;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;

namespace HwGarage.MVC.Services
{
    public class CarService
    {
        private readonly DbContext _db;

        public CarService(DbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult> AddCarAsync(
            User owner,
            string name,
            string description,
            string? photoUrl)
        {
            await using var tx = await _db.BeginTransactionAsync();

            var carsTx   = _db.Cars.UseTransaction(tx);
            var photosTx = _db.CarPhotos.UseTransaction(tx);

            var car = new Car
            {
                Owner_Id    = owner.Id,
                Name        = name,
                Description = description,
                Status      = "pending",      // ждёт модерации
                Created_At  = DateTime.UtcNow
            };

            int carId = await carsTx.InsertAsync(car);

            if (!string.IsNullOrEmpty(photoUrl))
            {
                var photoEntity = new CarPhoto
                {
                    Car_Id      = carId,
                    Photo_Url   = photoUrl,
                    Uploaded_At = DateTime.UtcNow
                };

                await photosTx.InsertAsync(photoEntity);
            }

            await tx.CommitAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> DeleteCarAsync(User user, int carId)
        {
            var car = await _db.Cars.FindAsync(carId);
            if (car == null || car.Owner_Id != user.Id)
            {
                return ServiceResult.Fail("Access denied");
            }

            await using var tx = await _db.BeginTransactionAsync();

            var carsTx   = _db.Cars.UseTransaction(tx);
            var photosTx = _db.CarPhotos.UseTransaction(tx);
            
            var photos = await photosTx
                .Where("car_id", carId)
                .ToListAsync();

            foreach (var photo in photos)
            {
                await photosTx.DeleteAsync(photo.Id);
            }

            await carsTx.DeleteAsync(carId);

            await tx.CommitAsync();
            return ServiceResult.Ok();
        }
    }
}
