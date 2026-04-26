using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using OBFSimple.Models;
using OBFSimple.Services;

namespace OBFSimple.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly IConfiguration _config;

    public AuthController(AuthService auth, IConfiguration config)
    {
        _auth = auth;
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = _auth.ValidateUser(request.Username, request.Password);
        if (user == null)
            return Unauthorized(new { message = "Invalid username or password" });

        var secret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("FullName", user.FullName),
            new("Role", user.Role)
        };

        if (user.UserType != null) claims.Add(new Claim("UserType", user.UserType));
        if (user.CollegeName != null) claims.Add(new Claim("CollegeName", user.CollegeName));

        var token = new JwtSecurityToken(
            issuer: "OBFSimple",
            audience: "OBFSimple",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return Ok(new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            UserType = user.UserType,
            CollegeName = user.CollegeName
        });
    }
}
