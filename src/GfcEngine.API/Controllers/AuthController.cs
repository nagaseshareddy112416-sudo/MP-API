using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using GfcEngine.Application.DTOs;
using GfcEngine.Domain.Entities;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Username) ??
                   await _userManager.FindByNameAsync(request.Username);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email/username or password." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email/username or password." });
        }

        // Fetch workspace roles
        var workspaceRoles = await _context.WorkspaceUserRoles
            .Where(r => r.UserId == user.Id)
            .Include(r => r.Workspace)
            .Select(r => new WorkspaceRoleDto
            {
                WorkspaceId = r.WorkspaceId,
                WorkspaceName = r.Workspace!.Name,
                Role = r.Role
            })
            .ToListAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
            new(ClaimTypes.Email, user.Email ?? ""),
            new("FullName", user.FullName)
        };

        var jwtKey = _configuration["Jwt:Secret"] ?? "GfcEngine_SuperSecretSecurityKey_AtLeast32BytesLong!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryHours = int.TryParse(_configuration["Jwt:ExpiryHours"], out var h) ? h : 24;

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "GfcEngine",
            audience: _configuration["Jwt:Audience"] ?? "GfcEngineClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds
        );

        return Ok(new LoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            UserId = user.Id,
            Username = user.UserName ?? user.Email ?? "",
            FullName = user.FullName,
            MustChangePassword = user.MustChangePassword,
            Roles = workspaceRoles
        });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Password updated successfully." });
    }

    [HttpPost("reset-admin")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetAdminPassword()
    {
        var user = await _userManager.FindByEmailAsync("admin@gfcengine.com");
        if (user == null) return NotFound("Admin user not found.");
        
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, "Admin@Gfc2026!");
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Admin password has been successfully reset to Admin@Gfc2026!" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<LoginResponseDto>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var workspaceRoles = await _context.WorkspaceUserRoles
            .Where(r => r.UserId == user.Id)
            .Include(r => r.Workspace)
            .Select(r => new WorkspaceRoleDto
            {
                WorkspaceId = r.WorkspaceId,
                WorkspaceName = r.Workspace!.Name,
                Role = r.Role
            })
            .ToListAsync();

        return Ok(new LoginResponseDto
        {
            Token = "",
            UserId = user.Id,
            Username = user.UserName ?? user.Email ?? "",
            FullName = user.FullName,
            MustChangePassword = user.MustChangePassword,
            Roles = workspaceRoles
        });
    }
}

