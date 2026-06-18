
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InternalMaintenance.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace InternalMaintenance.Api.Services;

public class JwtTokenService
{
    //Dùng để đọc Jwt: Key , Jwt: Issuer, Jwt: Audience
    // và Jwt: ExpiresInMinutes từ cofiguration. 
    private readonly IConfiguration  _configuration;
    public JwtTokenService (IConfiguration configuration)
    {
        _configuration = configuration;
    }

    //Nhận user đã được xác thực email/password thành công
    // Tạo Jwt access token cho user đó 
    public string GenerateAccessToken (User user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        //Định danh của Server tạo ra cái Token này
        var issuer = _configuration["Jwt:Issuer"]; 
        //Định danh của Client/ứng dụng được phép sử dụng Token này
        //Nó xác định xem "Tấm vé này dành cho ai và được dùng ở đâu"
        var audience = _configuration["Jwt:Audience"]; 
     
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("JWT key is missing");
        }

        if(user.Role is null)
        {
            throw new InvalidOperationException(
                "User role must be loaded before generating JWT"
            );
        }
        //Claims là các thông tin được nhúng token
        //Không đưa password hoặc PasswordHash vào đây 
        var claims = new List<Claim> {
             //Claim chuẩn JWT: token thuộc về user nào 
            new (
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()
             ),
             // Clam chuẩn .NET , thuận tiện lấy userId
             // Thông qua HttpContext.User.

             new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()
             ),
             new(
                ClaimTypes.Email,
                user.Email
             ),
             new(
                ClaimTypes.Name,
                user.FullName
             ),
             //Cho phép dùng [Authorize(Role="Admin")]
             new(
                ClaimTypes.Role,
                user.Role.Name
             ),
             // Custom claim phục vụ quy trình bắt buộc 
             // Đổi temporary password khi login lần đầu 
             new (
                "mustChangePassword",
                user.MustChangePassword.ToString()
             ),

             //Id riêng của token 
             new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()
             )
        };

        //Chuyển secret từ string thành byte[]
        //Để dùng cho thuật toán ký
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        //Kết hợp key và thuật toán HMAC SHA-256
        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256
        );

        var expiresInMinutes = int.TryParse(
            _configuration["Jwt:ExpiresInMinutes"],
            out var configuredMinutes
        ) ? configuredMinutes:60;

        // Tạo object JWT với issuer, audience,
        // claims, thời gian hết hạn và chữ ký
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: signingCredentials
        );

        // Chuyển JwtSecurity object thành chuỗi JWT
        return new JwtSecurityTokenHandler().WriteToken(token);

    }
}