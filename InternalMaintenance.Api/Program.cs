using InternalMaintenance.Api.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.Extensibility;

// Load biến môi trường từ file .env ở môi trường local
// Giúp không phải ghi connection string trực tiếp trong appsettings.json
Env.Load();
var builder = WebApplication.CreateBuilder(args);

//Lấy connection string từ cấu hình 
// Giá trị thật được lấy từ môi trường ConnectionStrings__DefaultConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if(string.IsNullOrWhiteSpace(connectionString)){
    throw new InvalidOperationException("Missing connection string: ConnectionStrings__DefaultConnection");
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.MapControllers();

// app.UseHttpsRedirection();


app.MapGet("/",()=>"Internal Maintenance API is running");
app.Run();


