using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Services;
using ReclamationService.Api.Infrastructure;
using ReclamationService.Domain.Enums;

namespace ReclamationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReclamationsController : ControllerBase
{
    private readonly ReclamationsService _reclamationService;

    public ReclamationsController(ReclamationsService reclamationService)
    {
        _reclamationService = reclamationService;
    }

    [HttpGet]
    public ActionResult<List<ReclamationDto>> GetAll([FromQuery] ReclamationStatus? status = null)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(_reclamationService.GetVisible(actor, status));
    }

    [HttpGet("query")]
    public ActionResult<PagedResult<ReclamationDto>> Query(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ReclamationStatus? status = null,
        [FromQuery] NamePriority? priority = null,
        [FromQuery] string? search = null)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(_reclamationService.QueryVisible(actor, status, priority, search, page, pageSize));
    }

    [HttpGet("{id}")]
    public ActionResult<ReclamationDto> GetById(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var reclamation = _reclamationService.GetById(id, actor);
        return Ok(reclamation);
    }

    [HttpGet("priority/{priority}")]
    public ActionResult<List<ReclamationDto>> GetByPriority(NamePriority priority)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(_reclamationService.GetByPriority(priority, actor));
    }

    [HttpGet("reference/{reference}")]
    public ActionResult<ReclamationDto> GetByReference(string reference)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var reclamation = _reclamationService.GetByReference(reference, actor);
        return Ok(reclamation);
    }

    [HttpPost]
    public async Task<ActionResult<ReclamationDto>> Create([FromBody] CreateReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var created = await _reclamationService.CreateAsync(dto, actor);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public ActionResult<ReclamationDto> Update(long id, [FromBody] UpdateReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = _reclamationService.Update(id, dto, actor);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        _reclamationService.Delete(id, actor);
        return NoContent();
    }

    [HttpGet("{id}/history")]
    public ActionResult<List<ReclamationHistoryDto>> GetHistory(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        return Ok(_reclamationService.GetHistory(id, actor));
    }

    [HttpPatch("{id}/assign")]
    public async Task<ActionResult<ReclamationDto>> Assign(long id, [FromBody] AssignReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.AssignAsync(id, dto, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/plan")]
    public async Task<ActionResult<ReclamationDto>> Plan(long id, [FromBody] PlanReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.PlanAsync(id, dto, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/start")]
    public async Task<ActionResult<ReclamationDto>> Start(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.StartAsync(id, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/resolve")]
    public async Task<ActionResult<ReclamationDto>> Resolve(long id, [FromBody] ResolveReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.ResolveAsync(id, dto, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/close")]
    public async Task<ActionResult<ReclamationDto>> Close(long id, [FromBody] CloseReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.CloseAsync(id, dto, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/cancel")]
    public async Task<ActionResult<ReclamationDto>> Cancel(long id)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.CancelAsync(id, actor);
        return Ok(updated);
    }

    [HttpPatch("{id}/reject")]
    public async Task<ActionResult<ReclamationDto>> Reject(long id, [FromBody] RejectReclamationDto dto)
    {
        var actor = User.ToCurrentUser(HttpContext);
        var updated = await _reclamationService.RejectAsync(id, dto, actor);
        return Ok(updated);
    }
}
