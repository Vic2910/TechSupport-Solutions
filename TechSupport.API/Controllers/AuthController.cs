using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TechSupport.API.Data;
using TechSupport.Contracts;
namespace TechSupport.API.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    // Hash válido para equiparar el costo de BCrypt cuando el usuario no existe.
    static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
    [AllowAnonymous, HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest input)
    {
        var user = await db.Usuarios.SingleOrDefaultAsync(x => x.NombreUsuario == input.NombreUsuario);
        var valid = BCrypt.Net.BCrypt.Verify(input.Password, user?.PasswordHash ?? DummyHash);
        if (user is null || !valid) return Unauthorized(new { message = "Credenciales incorrectas." });
        var expires = DateTime.UtcNow.AddMinutes(config.GetValue<int>("Jwt:ExpirationMinutes"));
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.NombreUsuario), new Claim(ClaimTypes.Role, user.Rol.ToString()) };
        var jwt = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims,
            expires: expires, signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(jwt), expires, user.Id, user.NombreUsuario, user.Rol));
    }
}
