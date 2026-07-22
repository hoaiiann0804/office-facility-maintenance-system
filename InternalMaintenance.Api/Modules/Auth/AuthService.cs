using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Common.Results;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Modules.Auth.Contracts;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Api.Modules.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(AppDbContext context, JwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user is null)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password"
            );
        }

        if (!user.IsActive)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator."
            );
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password"
            );
        }

        user.LastLoginAt = DateTime.UtcNow;

        var token = _jwtTokenService.GenerateAccessToken(user);
        var expiresInMinutes = _jwtTokenService.GetAccessTokenLifeTime();
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenLifeTime())
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return ServiceResult<LoginResponse>.Success(
            new LoginResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresInMinutes = expiresInMinutes,
                RefreshToken = refreshToken,
                MustChangePassword = user.MustChangePassword,
                User = BuildAuthUserResponse(user)
            }
        );
    }

    public async Task<ServiceResult<AuthUserResponse>> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return ServiceResult<AuthUserResponse>.Fail(
                StatusCodes.Status404NotFound,
                "User not found"
            );
        }

        if (!user.IsActive)
        {
            return ServiceResult<AuthUserResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator."
            );
        }

        return ServiceResult<AuthUserResponse>.Success(BuildAuthUserResponse(user));
    }

    public async Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return ServiceResult.Fail(
                StatusCodes.Status401Unauthorized,
                "User not found"
            );
        }

        if (!user.IsActive)
        {
            return ServiceResult.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator."
            );
        }

        var currentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
        if (!currentPasswordValid)
        {
            return ServiceResult.Fail(
                StatusCodes.Status400BadRequest,
                "Current password is incorrect"
            );
        }

        var isSamePassword = BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash);
        if (isSamePassword)
        {
            return ServiceResult.Fail(
                StatusCodes.Status400BadRequest,
                "New password must be different from current password"
            );
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult.Success(StatusCodes.Status200OK);
    }

    public async Task<ServiceResult<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedRefreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(rt => rt.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedRefreshToken is null)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token not found"
            );
        }

        if (storedRefreshToken.IsRevoked)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token revoked"
            );
        }

        if (storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token expired"
            );
        }

        var user = storedRefreshToken.User;
        if (!user.IsActive)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated."
            );
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenLifeTime())
        };

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return ServiceResult<RefreshTokenResponse>.Success(
            new RefreshTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresInMinutes = _jwtTokenService.GetAccessTokenLifeTime()
            }
        );
    }

    public async Task<ServiceResult> LogoutAsync(LogoutRequest request)
    {
        var storedRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedRefreshToken is null)
        {
            return ServiceResult.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token not found"
            );
        }

        if (storedRefreshToken.IsRevoked)
        {
            return ServiceResult.Success(StatusCodes.Status204NoContent);
        }

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult.Success(StatusCodes.Status204NoContent);
    }

    private static AuthUserResponse BuildAuthUserResponse(User user)
    {
        return new AuthUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RoleName = user.Role!.Name,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword
        };
    }
}
