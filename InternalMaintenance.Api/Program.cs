using System.Text;
using DotNetEnv;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Services;
using InternalMaintenance.Api.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Load biến môi trường từ file .env ở môi trường local.
// Nếu không có file .env thì app vẫn dùng configuration hiện tại.
Env.Load();
var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicyName = "FrontendCors";

// Lấy connection string từ cấu hình.
// Ưu tiên environment variable ConnectionStrings__DefaultConnection nếu có.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing connection string: ConnectionStrings__DefaultConnection");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: FrontendCorsPolicyName,
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Đọc JWT secret từ appsetting hoặc environment variable.
// Key này phải giống key đã dùng để ký token.
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// Nếu thiếu key thì backend không thể xác minh chữ ký JWT.
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT key is missing");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException("JWT issuer is missing");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT audience is missing");
}

// Đăng ký cơ chế xác thực cho ứng dụng.
// Mặc định hệ thống sẽ đọc token theo dạng Authorization: Bearer {accessToken}.
builder.Services
.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        ),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

// Đăng ký hệ thống phân quyền.
// Cho phép dùng [Authorize], [Authorize(Roles = "Admin")] hoặc policy.
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<ITicketCodeGenerator, TicketCodeGenerator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Migrate/seed là bước best-effort để tránh làm app chết trước khi bind port.
// Nếu DB đang lỗi, API vẫn lên để bạn thấy lỗi HTTP rõ ràng hơn thay vì ERR_CONNECTION_REFUSED.
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedData.InitializeAsync(dbContext);
}
catch (Exception ex)
{
    app.Logger.LogError(ex,
        "Database migration/seed failed during startup. The API will keep running, but DB-backed endpoints may fail.");
}

app.UseCors(FrontendCorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = "Internal Maintenance API",
    status = "running",
    environment = app.Environment.EnvironmentName
}));

app.Run();
