using Microsoft.AspNetCore.Mvc;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Services;
using ReclamationService.Domain.Enums;

namespace ReclamationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReclamationsController : ControllerBase
{
    private readonly ReclamationsService _reclamationService;

    public ReclamationsController(ReclamationsService reclamationService)
    {
        _reclamationService = reclamationService;
    }

    [HttpGet]
    public ActionResult<List<ReclamationDto>> GetAll()
    {
        return Ok(_reclamationService.GetAll());
    }

    [HttpGet("{id}")]
    public ActionResult<ReclamationDto> GetById(long id)
    {
        // NotFoundException is handled globally
        var reclamation = _reclamationService.GetById(id);
        return Ok(reclamation);
    }

    [HttpGet("priority/{priority}")]
    public ActionResult<List<ReclamationDto>> GetByPriority(NamePriority priority)
    {
        return Ok(_reclamationService.GetByPriority(priority));
    }

    [HttpGet("reference/{reference}")]
    public ActionResult<ReclamationDto> GetByReference(string reference)
    {
        // NotFoundException is handled globally
        var reclamation = _reclamationService.GetByReference(reference);
        return Ok(reclamation);
    }

    [HttpPost]
    public async Task<ActionResult<ReclamationDto>> Create([FromBody] CreateReclamationDto dto)
    {
        var created = await _reclamationService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public ActionResult<ReclamationDto> Update(long id, [FromBody] UpdateReclamationDto dto)
    {
        // NotFoundException is handled globally
        var updated = _reclamationService.Update(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(long id)
    {
        // NotFoundException is handled globally
        _reclamationService.Delete(id);
        return NoContent();
    }
}
