using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ClickHouseOptions>(builder.Configuration.GetSection("ClickHouse"));
builder.Services.AddSingleton<ClickHouseConnectionFactory>();
builder.Services.AddScoped<ITrackingRepository, ClickHouseTrackingRepository>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
