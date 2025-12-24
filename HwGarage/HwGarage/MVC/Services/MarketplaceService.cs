using System;
using System.Threading.Tasks;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;

namespace HwGarage.MVC.Services
{
    public class MarketplaceService
    {
        private readonly DbContext _db;

        public MarketplaceService(DbContext db)
        {
            _db = db;
        }
        
        public async Task<ServiceResult> CreateListingAsync(
            User seller,
            int carId,
            decimal price)
        {
            var car = await _db.Cars.FindAsync(carId);
            if (car == null || car.Owner_Id != seller.Id)
            {
                return ServiceResult.Fail("У вас нет прав на эту машинку.");
            }

            if (!string.Equals(car.Status, "available", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("Вы можете выставлять только доступные машинки (статус: available).");
            }

            var existingAuction = await _db.Auctions
                .Where("car_id", carId)
                .FirstOrDefaultAsync();

            if (existingAuction != null &&
                string.Equals(existingAuction.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("Эта машинка уже участвует в активном аукционе.");
            }

            var existingListing = await _db.Listings
                .Where("car_id", carId)
                .FirstOrDefaultAsync();

            if (existingListing != null && existingListing.Status == "active")
            {
                return ServiceResult.Fail("Эта машинка уже выставлена на продажу.");
            }

            await using var tx = await _db.BeginTransactionAsync();

            var listingsTx = _db.Listings.UseTransaction(tx);
            var carsTx     = _db.Cars.UseTransaction(tx);

            var listing = new MarketListing
            {
                Car_Id     = carId,
                Seller_Id  = seller.Id,
                Price      = price,
                Status     = "active",
                Created_At = DateTime.UtcNow
            };

            await listingsTx.InsertAsync(listing);

            car.Status = "on_sale";
            await carsTx.UpdateAsync(car.Id, car);

            await tx.CommitAsync();

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> BuyAsync(User buyer, int listingId)
        {
            await using var tx = await _db.BeginTransactionAsync();

            var listingsTx     = _db.Listings.UseTransaction(tx);
            var usersTx        = _db.Users.UseTransaction(tx);
            var carsTx         = _db.Cars.UseTransaction(tx);
            var transactionsTx = _db.Transactions.UseTransaction(tx);

            var listing = await listingsTx.FindAsync(listingId);
            if (listing == null || listing.Status != "active")
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail("Listing not found or not active");
            }

            var car = await carsTx.FindAsync(listing.Car_Id);
            if (car == null)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail("Car not found");
            }

            var seller = await usersTx.FindAsync(listing.Seller_Id);
            if (seller == null)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail("Seller not found");
            }

            if (buyer.Id == seller.Id)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail("You cannot buy your own car.");
            }

            if (buyer.Balance < listing.Price)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail("Not enough balance.");
            }

            buyer.Balance  -= listing.Price;
            seller.Balance += listing.Price;

            await usersTx.UpdateAsync(buyer.Id, buyer);
            await usersTx.UpdateAsync(seller.Id, seller);

            car.Owner_Id = buyer.Id;
            car.Status   = "available";
            await carsTx.UpdateAsync(car.Id, car);

            listing.Status = "sold";
            await listingsTx.UpdateAsync(listing.Id, listing);

            var transaction = new Transaction
            {
                Buyer_Id     = buyer.Id,
                Seller_Id    = seller.Id,
                Car_Id       = car.Id,
                Sale_Price   = listing.Price,
                Listing_Id   = listing.Id,
                Auction_Id   = null,
                Completed_At = DateTime.UtcNow
            };

            await transactionsTx.InsertAsync(transaction);

            await tx.CommitAsync();

            return ServiceResult.Ok();
        }
    }
}
