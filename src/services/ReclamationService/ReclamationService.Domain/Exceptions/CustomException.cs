namespace ReclamationService.Domain.Exceptions;

public abstract class CustomException : Exception
{
    public int StatusCode { get; }
    public List<string>? Errors { get; }

    protected CustomException(string message, int statusCode = 500, List<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}
