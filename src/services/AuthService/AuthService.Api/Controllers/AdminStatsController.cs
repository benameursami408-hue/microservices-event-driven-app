using AuthService.Application.DTOs;
using AuthService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/admin/stats")]
public class AdminStatsController : ControllerBase
{
    private readonly AdminUsersService _adminUsersService;

    public AdminStatsController(AdminUsersService adminUsersService)
    {
        _adminUsersService = adminUsersService;
    }

    [HttpGet("users")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<UserStatsDto>> GetUsers()
    {
        var stats = await _adminUsersService.GetStatsAsync();
        return Ok(stats);
    }
}
