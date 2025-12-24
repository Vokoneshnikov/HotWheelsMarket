using System;
using System.Globalization;

namespace HwGarage.MVC.Validation.Auction
{
    using HwGarage.MVC.Validation;

    public static class AuctionCreateValidator
    {
        private const decimal MaxAmount = 1_000_000m;

        public static ValidationResult Validate(
            string carIdRaw,
            string startPriceRaw,
            string bidStepRaw,
            string endsAtRaw,
            out int carId,
            out decimal startPrice,
            out decimal bidStep,
            out DateTime endsAt)
        {
            carId = default;
            startPrice = default;
            bidStep = default;
            endsAt = default;

            if (string.IsNullOrWhiteSpace(carIdRaw) ||
                string.IsNullOrWhiteSpace(startPriceRaw) ||
                string.IsNullOrWhiteSpace(bidStepRaw) ||
                string.IsNullOrWhiteSpace(endsAtRaw))
            {
                return ValidationResult.Fail("Заполните все поля.");
            }

            if (!int.TryParse(carIdRaw, out carId))
            {
                return ValidationResult.Fail("Некорректная машинка.");
            }

            if (!decimal.TryParse(startPriceRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out startPrice))
            {
                return ValidationResult.Fail("Некорректный формат стартовой цены.");
            }

            if (!decimal.TryParse(bidStepRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out bidStep))
            {
                return ValidationResult.Fail("Некорректный формат шага ставки.");
            }

            if (!DateTime.TryParse(endsAtRaw, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out endsAt))
            {
                return ValidationResult.Fail("Некорректный формат даты окончания.");
            }

            if (startPrice <= 0 || startPrice > MaxAmount)
            {
                return ValidationResult.Fail(
                    $"Стартовая цена должна быть больше 0 и меньше {MaxAmount}.");
            }

            if (bidStep <= 0 || bidStep > MaxAmount)
            {
                return ValidationResult.Fail(
                    $"Шаг ставки должен быть больше 0 и меньше {MaxAmount}.");
            }

            if (bidStep >= startPrice)
            {
                return ValidationResult.Fail(
                    "Шаг ставки не должен быть больше или равен стартовой цене.");
            }

            var now = DateTime.Now;

            if (endsAt <= now)
            {
                return ValidationResult.Fail(
                    "Время окончания аукциона должно быть в будущем.");
            }

            if (endsAt <= now.AddMinutes(5))
            {
                return ValidationResult.Fail(
                    "Аукцион должен длиться как минимум 5 минут.");
            }

            return ValidationResult.Ok();
        }
    }
}
