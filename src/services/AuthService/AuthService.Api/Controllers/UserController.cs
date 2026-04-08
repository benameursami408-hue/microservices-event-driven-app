using AuthService.Application.Mappers;
using AuthService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public ActionResult<List<AuthService.Application.DTOs.UserDto>> GetAll()
        {
            var items = _userService.GetAll().Select(u => u.ToDto()).ToList();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public ActionResult<AuthService.Application.DTOs.UserDto> GetById(long id)
        {
            var user = _userService.GetById(id);
            if (user == null) return NotFound();
            return Ok(user.ToDto());
        }
    }
}
