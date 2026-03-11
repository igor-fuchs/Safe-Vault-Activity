using System.Security.Claims;
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

    [Authorize(Roles = "Admin,User")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        UserResponse user = await _userService.GetUserByIdAsync(userId);
        return Ok(user);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!User.IsInRole("Admin"))
            return Unauthorized(new { error = "Only Admin users can access this endpoint." });

        List<UserResponse> users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }
}
