using AuthService.Domain.Exceptions;

namespace AuthService.Application.Exceptions;

public class BadRequestException : CustomException
{
    public BadRequestException(string message, List<string>? errors = null) 
        : base(message, 400, errors)
    {
    }
}
