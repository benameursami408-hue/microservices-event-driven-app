using AuthService.Application.Services;
using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public ActionResult<List<User>> GetAll()
        {
            return Ok(_userService.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<User> GetById(long id)
        {
            var user = _userService.GetById(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public ActionResult<User> Create(User user)
        {
            var createdUser = _userService.Create(user);
            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
        }

        [HttpPut("{id}")]
        public IActionResult Update(long id, User user)
        {
            if (id != user.Id) return BadRequest("L'ID ne correspond pas.");

            _userService.Update(user);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            var user = _userService.GetById(id);
            if (user == null) return NotFound();

            _userService.Delete(id);
            return NoContent();
        }
    }
}