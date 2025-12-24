using System.Text.RegularExpressions;
using HwGarage.MVC.Validation;

namespace HwGarage.MVC.Validation.Auth
{
    public class RegisterInput
    {
        public string FirstName { get; set; } = "";
        public string LastName  { get; set; } = "";
        public string Username  { get; set; } = "";
        public string Email     { get; set; } = "";
        public string Password  { get; set; } = "";
    }

    public class RegisterValidator : BaseValidator<RegisterInput>
    {
        private static readonly Regex NameRegex =
            new(@"^[A-Za-zА-Яа-яЁё\- ]{2,50}$", RegexOptions.Compiled);

        private static readonly Regex UsernameRegex =
            new(@"^[A-Za-z0-9_]{3,20}$", RegexOptions.Compiled);

        private static readonly Regex EmailRegex =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private static readonly Regex PasswordRegex =
            new(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$", RegexOptions.Compiled);

        protected override ValidationResult ValidateInternal(RegisterInput input)
        {
            var firstName = input.FirstName?.Trim() ?? "";
            var lastName  = input.LastName?.Trim() ?? "";
            var username  = input.Username?.Trim() ?? "";
            var email     = input.Email?.Trim() ?? "";
            var password  = input.Password ?? "";

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName)  ||
                string.IsNullOrWhiteSpace(username)  ||
                string.IsNullOrWhiteSpace(email)     ||
                string.IsNullOrWhiteSpace(password))
            {
                return ValidationResult.Fail("Заполните все поля.");
            }

            if (!NameRegex.IsMatch(firstName))
            {
                return ValidationResult.Fail(
                    "Имя может содержать только буквы, пробел и дефис (2–50 символов).");
            }

            if (!NameRegex.IsMatch(lastName))
            {
                return ValidationResult.Fail(
                    "Фамилия может содержать только буквы, пробел и дефис (2–50 символов).");
            }

            if (!UsernameRegex.IsMatch(username))
            {
                return ValidationResult.Fail(
                    "Имя пользователя может содержать только буквы латинского алфавита, цифры и '_' (3–20 символов).");
            }

            if (!EmailRegex.IsMatch(email))
            {
                return ValidationResult.Fail("Введите корректный email.");
            }

            if (!PasswordRegex.IsMatch(password))
            {
                return ValidationResult.Fail(
                    "Пароль должен быть не короче 8 символов и содержать хотя бы одну букву и одну цифру.");
            }

            return ValidationResult.Ok();
        }
    }
}
