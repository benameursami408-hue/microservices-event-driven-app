using ReclamationService.Domain.Exceptions;

namespace ReclamationService.Application.Exceptions;

public class NotFoundException : CustomException
{
    public NotFoundException(string message)
        : base(message, 404)
    {
    }
}
