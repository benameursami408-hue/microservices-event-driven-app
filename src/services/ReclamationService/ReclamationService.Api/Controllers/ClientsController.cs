using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReclamationService.Application.DTOs;
using ReclamationService.Application.Services;

namespace ReclamationService.API.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Roles = "ADMIN,SAV")]
public class ClientsController : ControllerBase
{
    private readonly ClientsService _clientsService;

    public ClientsController(ClientsService clientsService)
    {
        _clientsService = clientsService;
    }

    [HttpGet]
    public ActionResult<List<ClientDto>> GetAll()
    {
        return Ok(_clientsService.GetAll());
    }

    [HttpGet("{id:long}")]
    public ActionResult<ClientDto> GetById(long id)
    {
        return Ok(_clientsService.GetById(id));
    }

    [HttpPost]
    public ActionResult<ClientDto> Create([FromBody] CreateClientDto dto)
    {
        var created = _clientsService.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public ActionResult<ClientDto> Update(long id, [FromBody] UpdateClientDto dto)
    {
        return Ok(_clientsService.Update(id, dto));
    }

    [HttpPatch("{id:long}/status")]
    public IActionResult UpdateStatus(long id, [FromBody] UpdateClientStatusDto dto)
    {
        _ = id;
        _ = dto;
        return BadRequest(new
        {
            message = "Client active/inactive status is owned by AuthService user accounts. Use /api/admin/users/{id} to activate or deactivate the related account."
        });
    }
}
