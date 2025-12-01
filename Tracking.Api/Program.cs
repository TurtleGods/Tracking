using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Tracking.Api.Data;
using Tracking.Api.Models;
using Tracking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ClickHouseOptions>(builder.Configuration.GetSection("ClickHouse"));
builder.Services.AddSingleton<ClickHouseConnectionFactory>();
builder.Services.AddScoped<ITrackingRepository, ClickHouseTrackingRepository>();
builder.Services.Configure<ProductionOptions>(builder.Configuration.GetSection("Productions"));
builder.Services.AddSingleton<TrackingEventQueue>();
builder.Services.AddSingleton<ITrackingEventQueue>(sp => sp.GetRequiredService<TrackingEventQueue>());
builder.Services.AddSingleton<TrackingEventBackgroundService>();
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<TrackingEventBackgroundService>());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("__ModuleSessionCookie", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Cookie,
        Name = "__ModuleSessionCookie",
        Type = SecuritySchemeType.ApiKey,
        Description = "Paste the __ModuleSessionCookie value to authorize requests in Swagger."
    });

});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.EnablePersistAuthorization();
});

app.MapControllers();

app.Run();
