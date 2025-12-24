using HwGarage.MVC.Validation;

namespace HwGarage.MVC.Validation.Auth
{
    public class LoginInput
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginValidator : BaseValidator<LoginInput>
    {
        protected override ValidationResult ValidateInternal(LoginInput input)
        {
            var username = input.Username?.Trim() ?? "";
            var password = input.Password ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return ValidationResult.Fail("Заполните имя пользователя и пароль.");
            }

            if (username.Length < 3 || username.Length > 50)
            {
                return ValidationResult.Fail("Имя пользователя должно содержать от 3 до 50 символов.");
            }

            return ValidationResult.Ok();
        }
    }
}