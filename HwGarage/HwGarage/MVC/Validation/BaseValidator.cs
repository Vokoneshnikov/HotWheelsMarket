namespace HwGarage.MVC.Validation
{
    public abstract class BaseValidator<TInput>
    {
        public ValidationResult Validate(TInput input) => ValidateInternal(input);

        protected abstract ValidationResult ValidateInternal(TInput input);
    }
}