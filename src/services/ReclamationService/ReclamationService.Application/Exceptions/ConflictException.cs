using ReclamationService.Domain.Exceptions;

namespace ReclamationService.Application.Exceptions;

public class ConflictException : CustomException
{
    public ConflictException(string message, List<string>? errors = null)
        : base(message, 409, errors)
    {
    }
}
