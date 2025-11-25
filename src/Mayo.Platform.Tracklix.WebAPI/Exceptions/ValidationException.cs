namespace Mayo.Platform.Tracklix.WebAPI.Exceptions
{
    public class ValidationException : Exception
    {
        public string ErrorCode { get; }
        
        public ValidationException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
        
        public ValidationException(string message, string errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}