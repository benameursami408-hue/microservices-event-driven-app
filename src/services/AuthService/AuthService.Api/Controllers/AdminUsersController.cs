using AuthService.Application.DTOs;
using AuthService.Application.Services;
using AuthService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly AdminUsersService _adminUsersService;

    public AdminUsersController(AdminUsersService adminUsersService)
    {
        _adminUsersService = adminUsersService;
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN,SAV")]
    public ActionResult<List<UserDto>> GetAll([FromQuery] UserRole? role = null)
    {
        return Ok(_adminUsersService.GetAll(role));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var created = await _adminUsersService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<UserDto>> Update([FromRoute] long id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _adminUsersService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public ActionResult Delete([FromRoute] long id)
    {
        _adminUsersService.Delete(id);
        return NoContent();
    }
}
