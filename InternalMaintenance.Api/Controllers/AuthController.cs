
using System.Xml;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.DTOs.Auth;
using InternalMaintenance.Api.DTOs.Users;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/auth")]
public class AuthController:ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    
    private readonly CurrentUserService _currentUserService;
    
    private readonly JwtTokenService _jwtTokenService;
    
    public AuthController (
        AppDbContext context,
        IConfiguration configuration,
        CurrentUserService currentUserService,
        JwtTokenService jwtTokenService
    )
    {
        _context = context;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();
        
        var user = await _context.Users
        .Include(u=>u.Role)
        .Include(u=>u.Department)
        .FirstOrDefaultAsync(u => u.Email.ToLower() ==email);

        if(user is null)
        {
            return Unauthorized(
                new
                {
                    message="Invalid email or password"
                }
            );
        }

        if(!user.IsActive)
        {
            return Forbid();
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(
            request.Password, user.PasswordHash
        );

        if (!passwordValid)
        {
            return Unauthorized(
                new
                {
                    message="Invalid email or password"
                }
            );
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtTokenService.GenerateAccessToken(user);

        var expiresInMinutes = int.Parse(
            _configuration["Jwt:ExpiresInMinutes"] ?? "60"
        );

        return Ok (new LoginResponse
        {
            AccessToken = token,
            TokenType="Bearer",
            ExpiresInMinutes = expiresInMinutes,
            MustChangePassword = user.MustChangePassword,
            User = new AuthUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = user.Role!.Name,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.Name,
                IsActive = user.IsActive,
                MustChangePassword = user.MustChangePassword
            }
        });
    }
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserResponse>> Me()
    {
        var currentUserId = _currentUserService.UserId;

        var user = await _context.Users
        .Include(u => u.Role)
        .Include(u => u.Department)
        .FirstOrDefaultAsync(u => u.Id == currentUserId);

        if(user is null)
        {
            return Unauthorized(
                new
                {
                    message="User not found"
                }
            );
        }

        if (!user.IsActive)
        {
            return Forbid();
        }

        return Ok (new AuthUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RoleName = user.Role!.Name,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword

        });
    }
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<AuthUserResponse>> ChangePassword(ChangePasswordRequest request)
    {
        var currentUserId = _currentUserService.UserId;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);

        if(user is null)
        {
            return Unauthorized(
                new
                {
                    message = "User not found"
                }
            );
        }

        if(!user.IsActive)
        {
            return Forbid();
        }

        var currentPasswordValid = BCrypt.Net.BCrypt.Verify(
            request.CurrentPassword, user.PasswordHash
        );

        if (!currentPasswordValid)
        {
            return BadRequest(
                new
                {
                    message ="Current password is incorrect"
                }
            );
        }
        var isSamePassword = BCrypt.Net.BCrypt.Verify(
            request.NewPassword, user.PasswordHash
        );

        if(isSamePassword)
        {
            return BadRequest(
                new
                {
                    message="New password must be different from current password"
                }
            );
        }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(
            new
            {
                message="Password changed successfully. PLease login again. "
            }
        );
    }    
}