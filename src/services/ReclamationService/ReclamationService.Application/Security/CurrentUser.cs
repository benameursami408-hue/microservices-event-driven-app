namespace ReclamationService.Application.Security;

public record CurrentUser(
    long UserId,
    string Email,
    string FullName,
    string Role,
    string CorrelationId);
