using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SafeVault.DTOs;
using SafeVault.Models;

namespace SafeVault.Services;

public class UserService
{
    private readonly UserManager<User> _userManager;

    public UserService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            UserName = request.Username,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        return MapToResponse(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);

        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            throw new Exception("Invalid username or password.");

        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            User = MapToResponse(user)
        };
    }

    public async Task<List<UserResponse>> GetAllUsersAsync()
    {
        return await _userManager.Users
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.UserName!,
                Email = u.Email!
            })
            .ToListAsync();
    }

    private string GenerateJwtToken(User user)
    {
        var key = Environment.GetEnvironmentVariable("JWT_KEY")
            ?? throw new InvalidOperationException("JWT_KEY not found.");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.UserName!,
        Email = user.Email!
    };
}
