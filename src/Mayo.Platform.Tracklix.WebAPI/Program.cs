using Mayo.Platform.Tracklix.WebAPI.Services;
using Mayo.Platform.Tracklix.WebAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register custom services
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddTransient<EventCursorHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use global error handling middleware
app.UseMiddleware<GlobalErrorHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
