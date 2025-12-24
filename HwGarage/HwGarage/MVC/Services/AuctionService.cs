using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;

namespace HwGarage.MVC.Services
{
    public class AuctionService
    {
        private readonly DbContext _db;

        public AuctionService(DbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult> CreateAuctionAsync(
            User user,
            int carId,
            decimal startPrice,
            decimal bidStep,
            DateTime endsAt)
        {
            var car = await _db.Cars.FindAsync(carId);
            if (car == null || car.Owner_Id != user.Id)
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
                return ServiceResult.Fail("Для этой машинки уже существует активный аукцион.");
            }

            var existingListing = await _db.Listings
                .Where("car_id", carId)
                .FirstOrDefaultAsync();

            if (existingListing != null &&
                string.Equals(existingListing.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Fail("Эта машинка уже выставлена на продажу на торговой площадке.");
            }

            var now = DateTime.Now;

            await using var tx = await _db.BeginTransactionAsync();

            var auctionsTx = _db.Auctions.UseTransaction(tx);
            var carsTx     = _db.Cars.UseTransaction(tx);

            var auction = new Auction
            {
                Car_Id            = car.Id,
                Seller_Id         = user.Id,
                Start_Price       = startPrice,
                Bid_Step          = bidStep,
                Current_Bid       = 0,
                Current_Bidder_Id = null,
                Status            = "active",
                Started_At        = now,
                Ends_At           = endsAt
            };

            await auctionsTx.InsertAsync(auction);

            car.Status = "on_auction";
            await carsTx.UpdateAsync(car.Id, car);

            await tx.CommitAsync();

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> PlaceBidAsync(
            User user,
            int auctionId,
            decimal amount)
        {
            await using var tx = await _db.BeginTransactionAsync();

            var auctionsTx = _db.Auctions.UseTransaction(tx);
            var usersTx    = _db.Users.UseTransaction(tx);
            var bidsTx     = _db.Bids.UseTransaction(tx);

            var auction = await auctionsTx.FindAsync(auctionId);
            if (auction == null)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail("Auction not found");
            }

            var now = DateTime.Now;

            if (auction.Ends_At <= now || auction.Status != "active")
            {
                await tx.RollbackAsync();

                await FinalizeAuctionInternalAsync(auction);

                return ServiceResult.Fail("Auction finished");
            }

            if (auction.Seller_Id == user.Id)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail("You cannot bid on your own auction.");
            }

            if (auction.Current_Bidder_Id == user.Id)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail("You are already highest bidder.");
            }

            decimal min = (auction.Current_Bid > 0 ? auction.Current_Bid : auction.Start_Price)
                          + auction.Bid_Step;

            if (amount < min)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail($"Bid must be at least {min}");
            }

            if (auction.Current_Bid > 0 && auction.Current_Bidder_Id.HasValue)
            {
                var prevUser = await usersTx.FindAsync(auction.Current_Bidder_Id.Value);
                if (prevUser != null)
                {
                    prevUser.Balance += auction.Current_Bid;
                    await usersTx.UpdateAsync(prevUser.Id, prevUser);
                }
            }

            if (user.Balance < amount)
            {
                await tx.RollbackAsync();
                return ServiceResult.Fail("Not enough balance");
            }

            user.Balance -= amount;
            await usersTx.UpdateAsync(user.Id, user);

            var bid = new Bid
            {
                Auction_Id = auction.Id,
                Bidder_Id  = user.Id,
                Amount     = amount,
                Created_At = now
            };
            await bidsTx.InsertAsync(bid);

            auction.Current_Bid       = amount;
            auction.Current_Bidder_Id = user.Id;
            await auctionsTx.UpdateAsync(auction.Id, auction);

            await tx.CommitAsync();

            return ServiceResult.Ok();
        }

        public async Task FinalizeExpiredAuctionsAsync()
        {
            var now = DateTime.Now;

            var activeAuctions = await _db.Auctions
                .Where("status", "active")
                .ToListAsync();

            foreach (var auction in activeAuctions)
            {
                if (auction.Ends_At <= now)
                {
                    await FinalizeAuctionInternalAsync(auction);
                }
            }
        }

        private async Task FinalizeAuctionInternalAsync(Auction auction)
        {
            if (!string.Equals(auction.Status, "active", StringComparison.OrdinalIgnoreCase))
                return;

            await using var dbTx = await _db.BeginTransactionAsync();

            var auctionsTx     = _db.Auctions.UseTransaction(dbTx);
            var carsTx         = _db.Cars.UseTransaction(dbTx);
            var usersTx        = _db.Users.UseTransaction(dbTx);
            var transactionsTx = _db.Transactions.UseTransaction(dbTx);

            var freshAuction = await auctionsTx.FindAsync(auction.Id);
            if (freshAuction == null ||
                !string.Equals(freshAuction.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                await dbTx.RollbackAsync();
                return;
            }

            freshAuction.Status = "finished";
            await auctionsTx.UpdateAsync(freshAuction.Id, freshAuction);

            var car    = await carsTx.FindAsync(freshAuction.Car_Id);
            var seller = await usersTx.FindAsync(freshAuction.Seller_Id);

            // Возврат машинки владельцу, если нет ставок
            if (!freshAuction.Current_Bidder_Id.HasValue || freshAuction.Current_Bid <= 0)
            {
                if (car != null)
                {
                    car.Status = "available";
                    await carsTx.UpdateAsync(car.Id, car);
                }

                await dbTx.CommitAsync();
                return;
            }

            var buyer = await usersTx.FindAsync(freshAuction.Current_Bidder_Id.Value);

            if (car == null || seller == null || buyer == null)
            {
                if (car != null)
                {
                    car.Status = "available";
                    await carsTx.UpdateAsync(car.Id, car);
                }

                await dbTx.CommitAsync();
                return;
            }

            seller.Balance += freshAuction.Current_Bid;
            await usersTx.UpdateAsync(seller.Id, seller);

            car.Owner_Id = buyer.Id;
            car.Status   = "available";
            await carsTx.UpdateAsync(car.Id, car);

            var txEntity = new Transaction
            {
                Buyer_Id     = buyer.Id,
                Seller_Id    = seller.Id,
                Car_Id       = car.Id,
                Sale_Price   = freshAuction.Current_Bid,
                Auction_Id   = freshAuction.Id,
                Listing_Id   = null,
                Completed_At = DateTime.Now
            };

            await transactionsTx.InsertAsync(txEntity);

            await dbTx.CommitAsync();
        }
    }
}
