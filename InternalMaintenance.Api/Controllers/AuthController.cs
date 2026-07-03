
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
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    private readonly CurrentUserService _currentUserService;

    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
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
        .Include(u => u.Role)
        .Include(u => u.Department)
        .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user is null)
        {
            return Unauthorized(
                new
                {
                    message = "Invalid email or password"
                }
            );
        }

        if (!user.IsActive)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = "Your account has been deactivated. Please contact the administrator."
                }
            );
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(
            request.Password, user.PasswordHash
        );

        if (!passwordValid)
        {
            return Unauthorized(
                new
                {
                    message = "Invalid email or password"
                }
            );
        }

        user.LastLoginAt = DateTime.UtcNow;


        var token = _jwtTokenService.GenerateAccessToken(user);
        var expiresInMinutes = _jwtTokenService.GetAccessTokenLifeTime();
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiresInDays = _jwtTokenService.GetRefreshTokenLifeTime();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiresInDays)
        };

        _context.RefreshTokens.Add(refreshTokenEntity);

        return Ok(new LoginResponse
        await _context.SaveChangesAsync();
        return Ok(new LoginResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresInMinutes = expiresInMinutes,
            RefreshToken = refreshToken,
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

        if (user is null)
        {
            return NotFound(
                new
                {
                    message = "User not found"
                }
            );
        }

        if (!user.IsActive)
        {
            return Forbid();
        }

        return Ok(new AuthUserResponse
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
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var currentUserId = _currentUserService.UserId;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);

        if (user is null)
        {
            return Unauthorized(
                new
                {
                    message = "User not found"
                }
            );
        }

        if (!user.IsActive)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = "Your account has been deactivated. Please contact the administrator."
                }
            );
        }

        var currentPasswordValid = BCrypt.Net.BCrypt.Verify(
            request.CurrentPassword, user.PasswordHash
        );

        if (!currentPasswordValid)
        {
            return BadRequest(
                new
                {
                    message = "Current password is incorrect"
                }
            );
        }
        var isSamePassword = BCrypt.Net.BCrypt.Verify(
            request.NewPassword, user.PasswordHash
        );

        if (isSamePassword)
        {
            return BadRequest(
                new
                {
                    message = "New password must be different from current password"
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
                message = "Password changed successfully. PLease login again. "
            }
        );


    }
}

[AllowAnonymous]
[HttpPost("refresh-token")]
public async Task<ActionResult<RefreshTokenResponse>> RefreshToken(RefreshTokenRequest request)
{

    var storedRefreshToken = await _context.RefreshTokens
    .Include(rt => rt.User)
    .ThenInclude(rt => rt.Role)
    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
    if (storedRefreshToken is null)
    {
        return Unauthorized(new
        {
            message = "Refresh token not found"
        });
    }

    if (storedRefreshToken.IsRevoked)
    {
        return Unauthorized(new
        {
            message = "Refresh token revoked"
        });
    }

    if (storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
    {
        return Unauthorized(new
        {
            message = "Refresh token expired"
        });
    }
    var user = storedRefreshToken.User;
    if (!user.IsActive)
    {
        return StatusCode(
            StatusCodes.Status403Forbidden,
            new
            {
                message = "Your account has been deactivated."
            });
    }
    var accessToken = _jwtTokenService.GenerateAccessToken(user);
    var refreshToken = _jwtTokenService.GenerateRefreshToken();
    var expiresInMinutes = _jwtTokenService.GetAccessTokenLifeTime();
    var refreshTokenEntity = new RefreshToken
    {
        UserId = user.Id,
        Token = refreshToken,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(
            _jwtTokenService.GetRefreshTokenLifeTime())
    };
    storedRefreshToken.IsRevoked = true;
    storedRefreshToken.RevokedAt = DateTime.UtcNow;
    _context.RefreshTokens.Add(refreshTokenEntity);
    await _context.SaveChangesAsync();

    return Ok(
        new RefreshTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresInMinutes = expiresInMinutes,

        }
    );
}

[AllowAnonymous]
[HttpPost("logout")]
public async Task<IActionResult> Logout(LogoutRequest request)
{

    var storedRefreshToken = await _context.RefreshTokens
    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
    if (storedRefreshToken is null)
    {
        return Unauthorized(new
        {
            message = "Refresh token not found"
        });
    }

    if (storedRefreshToken.IsRevoked)
    {
        return NoContent();
    }
    var refreshToken = _jwtTokenService.GenerateRefreshToken();
    storedRefreshToken.IsRevoked = true;
    storedRefreshToken.RevokedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return NoContent();
}
    }
}