using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationsController(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Notification>>> Get([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        var items = await _notificationRepository.GetLatestAsync(take, cancellationToken);
        return Ok(items);
    }

    [HttpGet("latest")]
    public async Task<ActionResult<List<Notification>>> GetLatest([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        var items = await _notificationRepository.GetLatestAsync(take, cancellationToken);
        return Ok(items);
    }
}
