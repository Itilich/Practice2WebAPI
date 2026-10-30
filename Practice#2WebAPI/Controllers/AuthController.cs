using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practice_2WebAPI.Data;
using Practice_2WebAPI.DTOs;

namespace Practice_2WebAPI.Controllers;

[ApiController, Route("api/[controller]")]
public class AuthController(WebAPIContext db, IDataProtectionProvider protection) : ControllerBase
{
    private readonly PasswordHasher<AppUser> _hasher = new();
    private readonly IDataProtector _protector = protection.CreateProtector("Practice2.ApiToken.v1");

    [HttpPost("login"), Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Login == dto.Login);
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Unauthorized();
        return Ok(new { token = _protector.Protect($"{user.Login}|{user.Role}"), user.Login, user.Role });
    }

    [HttpPost("users"), Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 4)
            return BadRequest("Укажите логин и пароль длиной не менее 4 символов.");
        if (await db.Users.AnyAsync(u => u.Login == dto.Login)) return Conflict("Такой логин уже существует.");
        var user = new AppUser { Login = dto.Login.Trim(), Role = "User" };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Created("", new { user.Id, user.Login, user.Role });
    }
}
