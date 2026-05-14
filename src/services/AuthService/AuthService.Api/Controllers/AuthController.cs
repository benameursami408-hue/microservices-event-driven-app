using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Mappers;
using AuthService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    public const string AccessTokenCookieName = "sav_access_token";
    private readonly IAuthService _authService;
    private readonly UserService _userService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, UserService userService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _userService = userService;
        _environment = environment;
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthSensitive")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _authService.RegisterAsync(request);
        return Created("", result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthSensitive")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _authService.LoginAsync(request);
        Response.Cookies.Append(AccessTokenCookieName, result.Token, BuildCookieOptions());
        return Ok(new { user = result.User });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");
        if (!long.TryParse(idValue, out var userId)) return Unauthorized();

        var user = _userService.GetById(userId);
        if (user is null || !user.IsActive) return Unauthorized();
        return Ok(user.ToDto());
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AccessTokenCookieName, BuildCookieOptions());
        return NoContent();
    }

    private CookieOptions BuildCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = _environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddHours(1),
            Path = "/"
        };
    }
}
