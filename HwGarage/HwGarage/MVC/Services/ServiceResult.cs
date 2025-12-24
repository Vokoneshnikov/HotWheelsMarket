namespace HwGarage.MVC.Services
{
    public class ServiceResult
    {
        public bool Success { get; }
        public string? ErrorMessage { get; }

        protected ServiceResult(bool success, string? errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        public static ServiceResult Ok() => new(true, null);

        public static ServiceResult Fail(string message) => new(false, message);
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; }

        private ServiceResult(bool success, string? errorMessage, T? data)
            : base(success, errorMessage)
        {
            Data = data;
        }

        public static ServiceResult<T> Ok(T data) => 
            new(true, null, data);

        public new static ServiceResult<T> Fail(string message) => 
            new(false, message, default);
    }
}   