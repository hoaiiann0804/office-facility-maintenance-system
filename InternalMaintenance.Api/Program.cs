using InternalMaintenance.Api.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Load biến môi trường từ file .env ở môi trường local
// Giúp không phải ghi connection string trực tiếp trong appsettings.json
Env.Load();
var builder = WebApplication.CreateBuilder(args);

//Lấy connection string từ cấu hình 
// Giá trị thật được lấy từ môi trường ConnectionStrings__DefaultConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing connection string: ConnectionStrings__DefaultConnection");
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddOpenApi();

//Đọc JWT secrect từ appsetting hoặc enviroment variable.
// Key này phải giống key đã dùng để ký token
var jwtKey = builder.Configuration["Jwt:Key"];

//Nếu thiếu key phải dừng app ngay
//Không có key thì backend không thể xác minh chữ ký JWT.
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT key is missing");
}

//Đăng ký cơ chế xác thực cho ứng dụng
// Mặc định hệ thống sẽ đọc token theo dạng 
// Authorization: Bearer {accessToken}
builder.Services
.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    //Các điều kiện token phải vượt qua để xem hợp lệ

    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Kiểm tra token có đúng do hệ thống này phát hành không.
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        //Kiểm tra token có đúng dành cho client/hệ thống này không.
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        //Kiểm tra token còn hạn hay hết hạn.
        ValidateLifetime = true,
        //Kiểm tra chữ ký token có hợp lệ không
        ValidateIssuerSigningKey = true,
        // Dùng secrect key để xác minh token
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        )

    };
});

// Đăng ký hệ thống phân quyền 
// Cho phép dùng [Authorize], [Authorize(Role="Admin")] hoặc policy 
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<CurrentUserService>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}
//Gọi SeedData 
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(dbContext);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// app.UseHttpsRedirection();


app.MapGet("/", () => "Internal Maintenance API is running");
// var hash = BCrypt.Net.BCrypt.HashPassword("Temp@123456");
// Console.WriteLine(hash);
app.Run();



