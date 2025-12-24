using System;
using System.Globalization;

namespace HwGarage.MVC.Validation.Marketplace
{
    using HwGarage.MVC.Validation;

    public static class MarketplaceAddValidator
    {
        private const decimal MaxAmount = 1_000_000m;

        public static ValidationResult Validate(
            string carIdRaw,
            string priceRaw,
            out int carId,
            out decimal price)
        {
            carId = default;
            price = default;

            if (string.IsNullOrWhiteSpace(carIdRaw) ||
                string.IsNullOrWhiteSpace(priceRaw))
            {
                return ValidationResult.Fail("Заполните все поля.");
            }

            if (!int.TryParse(carIdRaw, out carId))
            {
                return ValidationResult.Fail("Некорректная машинка.");
            }

            if (!decimal.TryParse(priceRaw, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out price))
            {
                return ValidationResult.Fail("Некорректный формат цены.");
            }

            if (price <= 0 || price > MaxAmount)
            {
                return ValidationResult.Fail(
                    $"Цена должна быть больше 0 и меньше {MaxAmount}.");
            }

            return ValidationResult.Ok();
        }
    }
}