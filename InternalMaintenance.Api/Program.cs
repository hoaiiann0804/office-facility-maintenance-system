using InternalMaintenance.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

await app.UseApplicationPipelineAsync();

app.Run();
