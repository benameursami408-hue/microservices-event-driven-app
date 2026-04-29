using InterventionService.Api.Infrastructure;
using InterventionService.Application.DTOs;
using InterventionService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterventionService.Api.Controllers;

[ApiController]
[Route("api/planning")]
[Authorize]
public class PlanningController : ControllerBase
{
    private readonly PlanningService _planningService;

    public PlanningController(PlanningService planningService)
    {
        _planningService = planningService;
    }

    [HttpGet("requests")]
    [Authorize(Roles = "SAV,ADMIN")]
    public async Task<ActionResult<List<PlanningRequestDto>>> GetRequests(CancellationToken cancellationToken)
    {
        return Ok(await _planningService.GetRequestsAsync(cancellationToken));
    }

    [HttpGet("requests/{id:guid}")]
    [Authorize(Roles = "SAV,ADMIN")]
    public async Task<ActionResult<PlanningRequestDto>> GetRequest(Guid id, CancellationToken cancellationToken)
    {
        var item = await _planningService.GetRequestAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointments(
        [FromQuery] long? reclamationId,
        [FromQuery] long? technicianId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _planningService.QueryAppointmentsAsync(actor, reclamationId, technicianId, from, to, cancellationToken));
    }

    [HttpGet("appointments/{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(Guid id, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var item = await _planningService.GetAppointmentAsync(id, actor, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("reclamations/{reclamationId:long}")]
    public async Task<ActionResult<AppointmentDto>> GetByReclamation(long reclamationId, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var item = await _planningService.GetByReclamationAsync(reclamationId, actor, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("technicians/{technicianId:long}/capacity")]
    [Authorize(Roles = "SAV,ADMIN,ST")]
    public async Task<ActionResult<TechnicianCapacityDto>> GetCapacity(long technicianId, [FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _planningService.GetTechnicianCapacityAsync(technicianId, actor, date, cancellationToken));
    }

    [HttpGet("technicians/{technicianId:long}/agenda")]
    [Authorize(Roles = "SAV,ADMIN,ST")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAgenda(
        long technicianId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _planningService.GetTechnicianAgendaAsync(technicianId, actor, from, to, cancellationToken));
    }

    [HttpPost("appointments")]
    [Authorize(Roles = "SAV,ADMIN")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _planningService.CreateAppointmentAsync(dto, actor, cancellationToken));
    }

    [HttpPost("appointments/{id:guid}/assign-technician")]
    [Authorize(Roles = "SAV,ADMIN")]
    public async Task<ActionResult<AppointmentDto>> AssignTechnician(Guid id, [FromBody] AssignTechnicianDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _planningService.AssignTechnicianAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("appointments/{id:guid}/confirm")]
    [Authorize(Roles = "SAV,ADMIN")]
    public async Task<ActionResult<AppointmentDto>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _planningService.ConfirmAppointmentAsync(id, actor, cancellationToken));
    }

    [HttpPost("appointments/{id:guid}/reschedule")]
    [Authorize(Roles = "SAV,ADMIN,ST")]
    public async Task<ActionResult<AppointmentDto>> Reschedule(Guid id, [FromBody] RescheduleAppointmentDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _planningService.RescheduleAppointmentAsync(id, dto, actor, cancellationToken));
    }

    [HttpPost("appointments/{id:guid}/cancel")]
    [Authorize(Roles = "SAV,ADMIN")]
    public async Task<ActionResult<AppointmentDto>> Cancel(Guid id, [FromBody] CancelAppointmentDto dto, CancellationToken cancellationToken)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(await _planningService.CancelAppointmentAsync(id, dto, actor, cancellationToken));
    }
}
