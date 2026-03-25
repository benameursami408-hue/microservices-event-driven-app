using AuthService.Domain.Exceptions;

namespace AuthService.Application.Exceptions;

public class NotFoundException : CustomException
{
    public NotFoundException(string message) 
        : base(message, 404)
    {
    }
}
