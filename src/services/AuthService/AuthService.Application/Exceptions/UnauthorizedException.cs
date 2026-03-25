using AuthService.Domain.Exceptions;

namespace AuthService.Application.Exceptions;

public class UnauthorizedException : CustomException
{
    public UnauthorizedException(string message) 
        : base(message, 401)
    {
    }
}
