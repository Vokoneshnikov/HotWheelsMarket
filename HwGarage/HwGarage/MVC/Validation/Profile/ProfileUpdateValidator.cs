using HwGarage.MVC.Validation;

namespace HwGarage.MVC.Validation.Profile
{
    public class ProfileUpdateInput
    {
        public string FirstName { get; set; } = "";
        public string LastName  { get; set; } = "";
        public string Email     { get; set; } = "";
    }

    public class ProfileUpdateValidator : BaseValidator<ProfileUpdateInput>
    {
        protected override ValidationResult ValidateInternal(ProfileUpdateInput input)
        {
            var firstName = input.FirstName?.Trim() ?? "";
            var lastName  = input.LastName?.Trim() ?? "";
            var email     = input.Email?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email))
            {
                return ValidationResult.Fail("Email обязателен.");
            }

            if (email.Length > 100 || email.Contains(" ") || !email.Contains("@"))
            {
                return ValidationResult.Fail("Введите корректный email.");
            }

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                if (firstName.Length < 2 || firstName.Length > 50)
                {
                    return ValidationResult.Fail("Имя должно быть от 2 до 50 символов.");
                }
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                if (lastName.Length < 2 || lastName.Length > 50)
                {
                    return ValidationResult.Fail("Фамилия должна быть от 2 до 50 символов.");
                }
            }

            return ValidationResult.Ok();
        }
    }
}