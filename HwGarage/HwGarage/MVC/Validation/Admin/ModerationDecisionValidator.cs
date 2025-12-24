using HwGarage.MVC.Validation;

namespace HwGarage.MVC.Validation.Admin
{
    public static class ModerationDecisionValidator
    {
        public static ValidationResult Validate(
            string carIdRaw,
            string decisionRaw,
            out int carId,
            out string decision)
        {
            carId = default;
            decision = string.Empty;

            if (!int.TryParse(carIdRaw, out carId))
            {
                return ValidationResult.Fail("Invalid car id");
            }

            decision = (decisionRaw ?? string.Empty).Trim().ToLowerInvariant();

            if (decision != "approve" && decision != "reject")
            {
                return ValidationResult.Fail("Unknown decision");
            }

            return ValidationResult.Ok();
        }
    }
}