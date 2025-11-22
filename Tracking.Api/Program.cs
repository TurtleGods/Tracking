using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Data;
using Tracking.Api.Requests;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ClickHouseOptions>(builder.Configuration.GetSection("ClickHouse"));
builder.Services.AddSingleton<ClickHouseConnectionFactory>();
builder.Services.AddScoped<ITrackingRepository, ClickHouseTrackingRepository>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/entities", async ([FromQuery] int limit, ITrackingRepository repository, CancellationToken cancellationToken) =>
{
    var take = NormalizeLimit(limit);
    var entities = await repository.GetMainEntitiesAsync(take, cancellationToken);
    return Results.Ok(entities);
});

app.MapPost("/entities", async ([FromBody] CreateMainEntityRequest request, ITrackingRepository repository, CancellationToken cancellationToken) =>
{
    var entity = request.ToMainEntity();
    await repository.InsertMainEntityAsync(entity, cancellationToken);
    return Results.Created($"/entities/{entity.EntityId}", entity);
});

app.MapDelete("/entities/{entityId:guid}", async (Guid entityId, ITrackingRepository repository, CancellationToken cancellationToken) =>
{
    await repository.DeleteEntityCascadeAsync(entityId, cancellationToken);
    return Results.NoContent();
});

app.MapGet("/entities/{entityId:guid}/sessions", async (Guid entityId, [FromQuery] int limit, ITrackingRepository repository, CancellationToken cancellationToken) =>
{
    var take = NormalizeLimit(limit);
    var sessions = await repository.GetSessionsAsync(entityId, take, cancellationToken);
    return Results.Ok(sessions);
});

app.MapPost("/entities/{entityId:guid}/sessions", async (Guid entityId, [FromBody] CreateTrackingSessionRequest request, ITrackingRepository repository, CancellationToken cancellationToken) =>
{
    var session = request.ToTrackingSession(entityId);
    await repository.InsertSessionAsync(session, cancellationToken);
    return Results.Created($"/entities/{entityId}/sessions/{session.Id}", session);
});

app.MapGet("/entities/{entityId:guid}/events", async (Guid entityId, [FromQuery] int limit, ITrackingRepository repository, CancellationToken cancellationToken) =>
{
    var take = NormalizeLimit(limit);
    var eventsForEntity = await repository.GetEventsAsync(entityId, take, cancellationToken);
    return Results.Ok(eventsForEntity);
});

app.MapPost("/entities/{entityId:guid}/events", async (Guid entityId, [FromBody] CreateTrackingEventRequest request, ITrackingRepository repository, CancellationToken cancellationToken) =>
{
    var trackingEvent = request.ToTrackingEvent(entityId);
    await repository.InsertEventAsync(trackingEvent, cancellationToken);
    return Results.Created($"/entities/{entityId}/events/{trackingEvent.Id}", trackingEvent);
});

app.Run();

static int NormalizeLimit(int limit) => limit switch
{
    < 1 => 50,
    > 500 => 500,
    _ => limit
};
