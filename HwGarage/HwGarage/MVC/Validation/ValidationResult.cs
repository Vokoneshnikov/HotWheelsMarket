namespace HwGarage.MVC.Validation
{
    public class ValidationResult
    {
        public bool IsValid => string.IsNullOrEmpty(ErrorMessage);
        public string? ErrorMessage { get; }

        private ValidationResult(string? errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Ok() => new ValidationResult(null);

        public static ValidationResult Fail(string message) =>
            new ValidationResult(string.IsNullOrWhiteSpace(message)
                ? "Validation failed."
                : message);
    }
}