using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Api.Infrastructure;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
        var actor = User.ToCurrentUser();
        take = Math.Clamp(take, 1, 200);

        // ADMIN can inspect the whole feed; others only see their own notifications.
        var items = actor.IsInRole("ADMIN")
            ? await _notificationRepository.GetLatestAsync(take, cancellationToken)
            : await _notificationRepository.GetLatestForUserAsync(actor.UserId, take, cancellationToken);

        return Ok(items);
    }

    [HttpGet("latest")]
    public async Task<ActionResult<List<Notification>>> GetLatest([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        return await Get(take, cancellationToken);
    }

    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id, CancellationToken cancellationToken = default)
    {
        var actor = User.ToCurrentUser();
        var success = await _notificationRepository.MarkAsReadAsync(id, actor.UserId, actor.IsInRole("ADMIN"), cancellationToken);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult<object>> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var actor = User.ToCurrentUser();
        var updated = await _notificationRepository.MarkAllAsReadAsync(actor.UserId, actor.IsInRole("ADMIN"), cancellationToken);
        return Ok(new { updated });
    }
}
