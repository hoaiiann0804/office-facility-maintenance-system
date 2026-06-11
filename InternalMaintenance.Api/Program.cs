using InternalMaintenance.Api.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

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
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();


app.MapGet("/",()=>"Internal Maintenance API is running");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
