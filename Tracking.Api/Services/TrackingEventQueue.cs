using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tracking.Api.Data;
using Tracking.Api.Models;
using Tracking.Api.Requests;

namespace Tracking.Api.Services;

public sealed record TrackingEventCommand(Guid SessionId, Guid CompanyId, Guid EmployeeId, CreateTrackingEventRequest Request);

public interface ITrackingEventQueue
{
    Task<bool> EnqueueAsync(TrackingEventCommand command, CancellationToken cancellationToken);
}

public sealed class TrackingEventQueue : ITrackingEventQueue
{
    private readonly Channel<TrackingEventCommand> _channel;
    public ChannelReader<TrackingEventCommand> Reader => _channel.Reader;
    public int Capacity { get; }
    public long Enqueued => Interlocked.Read(ref _enqueued);
    public long Dropped => Interlocked.Read(ref _dropped);
    public int CurrentDepth => _channel.Reader.Count;

    private long _enqueued;
    private long _dropped;

    public TrackingEventQueue()
    {
        // Bounded queue to prevent unbounded memory growth under bursty load.
        Capacity = 50_0000;
        _channel = Channel.CreateBounded<TrackingEventCommand>(new BoundedChannelOptions(Capacity)
        {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite
        });
    }

    public Task<bool> EnqueueAsync(TrackingEventCommand command, CancellationToken cancellationToken)
    {
        var accepted = _channel.Writer.TryWrite(command);
        if (accepted)
        {
            Interlocked.Increment(ref _enqueued);
        }
        else
        {
            Interlocked.Increment(ref _dropped);
        }
        return Task.FromResult(accepted);
    }
}

public sealed class TrackingEventBackgroundService : BackgroundService
{
    private readonly TrackingEventQueue _queue;
    private readonly ProductionOptions _productionOptions;
    private readonly ILogger<TrackingEventBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly string[] DefaultProductionCodes = new[] { "PT", "PY", "FD" };

    public TrackingEventBackgroundService(
        TrackingEventQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<ProductionOptions> productionOptions,
        ILogger<TrackingEventBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _productionOptions = productionOptions.Value;
        _logger = logger;
    }

    public long Processed => Interlocked.Read(ref _processed);
    public long Failed => Interlocked.Read(ref _failed);
    private long _processed;
    private long _failed;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var command in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(command, stoppingToken);
                Interlocked.Increment(ref _processed);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failed);
                _logger.LogError(ex, "Failed to process tracking event for company {CompanyId}", command.CompanyId);
            }
        }
    }

    private async Task ProcessAsync(TrackingEventCommand command, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITrackingRepository>();

        var production = command.Request.Production;
        var existingSession = await repository.GetEventBySessionIdAsync(command.SessionId, cancellationToken);

        TrackingSession session;
        if (existingSession is null)
        {
            var entity = await GetOrCreateEntityAsync(repository, command.CompanyId, production, cancellationToken);
            var startedAt = command.Request.Timestamp ?? DateTime.UtcNow;
            session = new TrackingSession
            {
                SessionId = command.SessionId,
                EntityId = entity.EntityId,
                EmployeeId = command.EmployeeId,
                CompanyId = command.CompanyId,
                StartedAt = startedAt,
                LastActivityAt = startedAt,
                EndedAt = null,
                CreatedAt = DateTime.UtcNow
            };

            await repository.InsertSessionAsync(session, cancellationToken);
        }
        else
        {
            session = existingSession;
        }

        var trackingEvent = command.Request.ToTrackingEvent(session.EntityId, session.SessionId, session.EmployeeId, session.CompanyId);
        await repository.InsertEventAsync(trackingEvent, cancellationToken);
    }

    private async Task<MainEntity> GetOrCreateEntityAsync(ITrackingRepository repository, Guid companyId, string production, CancellationToken cancellationToken)
    {
        var existing = await repository.GetMainEntityByCompanyAndProductionAsync(companyId, production, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var productions = _productionOptions.Codes is { Length: > 0 } ? _productionOptions.Codes : DefaultProductionCodes;
        foreach (var prod in productions)
        {
            var entity = new MainEntity
            {
                EntityId = CreateDeterministicEntityId(prod, companyId),
                CompanyId = companyId,
                Production = prod,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await repository.InsertMainEntityAsync(entity, cancellationToken);
        }

        var created = await repository.GetMainEntityByCompanyAndProductionAsync(companyId, production, cancellationToken);
        return created!;
    }

    private static Guid CreateDeterministicEntityId(string production, Guid companyId)
    {
        var key = $"{production}:{companyId}".ToLowerInvariant();
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
        return new Guid(bytes);
    }
}
