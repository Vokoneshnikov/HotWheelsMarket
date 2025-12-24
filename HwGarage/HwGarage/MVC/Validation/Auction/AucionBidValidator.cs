using System.Globalization;

namespace HwGarage.MVC.Validation.Auction
{
    using HwGarage.MVC.Validation;

    public static class AuctionBidValidator
    {
        public static ValidationResult Validate(
            string auctionIdRaw,
            string amountRaw,
            out int auctionId,
            out decimal amount)
        {
            auctionId = default;
            amount = default;

            if (!int.TryParse(auctionIdRaw, out auctionId))
            {
                return ValidationResult.Fail("Invalid auction id");
            }

            if (!decimal.TryParse(amountRaw, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out amount))
            {
                return ValidationResult.Fail("Invalid amount");
            }

            if (amount <= 0)
            {
                return ValidationResult.Fail("Invalid amount");
            }

            return ValidationResult.Ok();
        }
    }
}