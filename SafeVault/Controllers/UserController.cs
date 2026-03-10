using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeVault.DTOs;
using SafeVault.Services;

namespace SafeVault.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        UserResponse user = await _userService.RegisterAsync(request);
        return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        LoginResponse response = await _userService.LoginAsync(request);
        return Ok(response);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        List<UserResponse> users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }
}
