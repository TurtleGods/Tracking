using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

var baseUrl = Environment.GetEnvironmentVariable("TARGET_BASE") ?? "http://localhost:8080";
var total = int.TryParse(Environment.GetEnvironmentVariable("TOTAL_EVENTS") ?? "10000", out var t) ? t : 100000;
var concurrency = int.TryParse(Environment.GetEnvironmentVariable("CONCURRENCY") ?? "64", out var c) ? c : 64;
var progress = int.TryParse(Environment.GetEnvironmentVariable("PROGRESS_STEP") ?? "1000", out var p) && p > 0 ? p : 1000;
var sessionIdEnv = Environment.GetEnvironmentVariable("SESSION_ID");

var companyId = Guid.NewGuid();
var employeeId = Guid.NewGuid();
var jwtSecret = Environment.GetEnvironmentVariable("LOAD_TEST_JWT_SECRET") ?? "dev-secret";
var progressLock = new object();

var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web); // camelCase for requests
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
client.DefaultRequestHeaders.Add("Cookie", $"__ModuleSessionCookie=eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjE2MTBjMTQ1ODdjMGUzY2U0YTk1N2IyMzlhODM3MzIwIn0.eyJpc3MiOiJodHRwczovL3VhdC1hc2lhYXV0aC5tYXlvaHIuY29tL3N0cyIsImlhdCI6MTc2NDU1MzYzNCwiYXVkIjoiNDNmNWJjZGEtZjM0YS00YzA1LTk3ZGMtZTkxZWQxNTI3MzYzIiwibm9uY2UiOiJPVEF6WWprNE5EY3RabVJrT0MwMFl6STFMVGd3TmpRdE16aGtOMll3TURkalpqSXpRRzFoZVc4M056Y3RZVFF6TWpRMVFERTNOalExTlRNMk16UT0iLCJleHAiOjE3NjUxNTg0MzMsIm5iZiI6MTc2NDU1MzYzMywianRpIjoiY2UyOGZlMTRkYWEzNDQ2OTlkMTFjYzdmYmQ3ZWMxZmIiLCJzdWIiOiIwNzcxOTllNWIyMTg0YmM1YjJmMWNjZTBjNDEwYmUyNjgzNTFiOWViYWVlMjQ4MGRiNzAxMzg3OTA4NTkxNjhiIiwiYW1yIjoicHdkIiwiaXBhZGRyIjoiMTAuNDAuMTIuNSIsIm9pZCI6ImNkMzg2YzE2LTE1MmEtNGZjYi1iN2FlLThmMTNhZGM3MWZkNCIsInVpZCI6ImNkMzg2YzE2LTE1MmEtNGZjYi1iN2FlLThmMTNhZGM3MWZkNCIsImlkcCI6Im1heW8gSFJNIiwiZWlkIjoiMzM3NTllYmUtYzk4ZC00MzYxLWEwYTItZTZmMzE0N2U5Y2Y4IiwiY2lkIjoiZGMzNGFmZjYtMmYwYy00OGUwLTk1MTMtZDYyNTE1Yjk2MjViIiwidWJtIjoiTm9uZSIsInJ0IjowLCJzZXAiOjEsInpvbmVpbmZvIjoiKzA4OjAwIiwiaXNGaWRvMlJlZ2lzdGVyZWQiOiIifQ.cx1uqaYJBoqJkcOCQlOTU9YutV8MzwEjP9p6pJYD8t2LFS0FEO_Zr63q1WVrfsFLMI3X05xwVHKHxzAqdW3u1lnjQHb5owqbrlWj2MEcACNONStQNJytuyU-9GLBA4WKgSLskhiOLqRqH-_Mx0axEiViLhGLcP4MmEVA-EPgC6AFnQfgJmIwiAT0IV0NP8VnGG31hd_K00MPhgXNmAGYacorc3oMHIBiM2DIugvmn00rMikD2O_TcGMlAue1u9lXigNIEQFI-yUeDUQazgJFJO72JEaCeG4kgbsCMcrpQkXMzT53G43617VLxWkcM9RxsHc1Y9NCS3Ae8LL3zIqAGA"); // API extracts cid/eid from this cookie

Console.WriteLine($"Base={baseUrl} total={total} concurrency={concurrency}");

var sessionId = Guid.TryParse(sessionIdEnv, out var parsedSession) ? parsedSession : Guid.Empty;
var entityId = Guid.Empty;
var handshakeSent = 0;

if (sessionId == Guid.Empty)
{
    var handshake = new TrackingEventRequest
    {
        SessionId = Guid.Empty, // let API create session (fallback route will set a guid)
        EventType = "behavior",
        EventName = "pt_event_handshake",
        PageName = "handshake",
        ComponentName = "init",
        Timestamp = DateTime.UtcNow,
        Refer = "https://app.local/landing",
        ExposeTime = 0,
        EmployeeId = employeeId,
        CompanyId = companyId,
        DeviceType = "web",
        OsVersion = "macOS 15",
        BrowserVersion = "Chrome 130",
        NetworkType = "wifi",
        NetworkEffectiveType = "4g",
        PageUrl = "https://app.local/handshake",
        PageTitle = "Handshake",
        ViewportHeight = 900,
        Properties = """{"type":"handshake"}"""
    };

    var handshakeResponse = await SendEventAsync(client, jsonOpts, sessionId, handshake);
    sessionId = handshakeResponse.SessionId;
    entityId = handshakeResponse.EntityId;
    handshakeSent = 1;
    Console.WriteLine($"Handshake created session={sessionId} entity={entityId} companyId={companyId}");
}
else
{
    Console.WriteLine($"Using provided session={sessionId} companyId={companyId}");
}

var channel = Channel.CreateBounded<int>(concurrency * 4);
var sw = Stopwatch.StartNew();
long failures = 0;
long sent = handshakeSent;

var workers = Enumerable.Range(0, concurrency).Select(async _ =>
{
    await foreach (var i in channel.Reader.ReadAllAsync())
    {
        var payload = new TrackingEventRequest
        {
            SessionId = sessionId,
            EventType = "behavior",
            EventName = $"pt_event_{i % 10}",
            PageName = "dashboard",
            ComponentName = "chart",
            Timestamp = DateTime.UtcNow,
            Refer = "https://app.local/landing",
            ExposeTime = Random.Shared.Next(0, 3000),
            EmployeeId = employeeId,
            CompanyId = companyId,
            DeviceType = "web",
            OsVersion = "macOS 15",
            BrowserVersion = "Chrome 130",
            NetworkType = "wifi",
            NetworkEffectiveType = "4g",
            PageUrl = "https://app.local/dashboard",
            PageTitle = "Dashboard",
            ViewportHeight = 900,
            Properties = """{""variant"":""A"",""cta"":""signup""}"""
        };

        try
        {
            var response = await SendEventAsync(client, jsonOpts, sessionId, payload);
            if (response.SessionId == Guid.Empty) Interlocked.Increment(ref failures);
        }
        catch
        {
            Interlocked.Increment(ref failures);
        }

        var count = Interlocked.Increment(ref sent);
        if (count % progress == 0 || count == total)
        {
            lock (progressLock)
            {
                Console.Write($"\rProgress: {count}/{total}");
                if (count == total)
                {
                    Console.WriteLine();
                }
            }
        }
    }
});

_ = Task.Run(async () =>
{
    for (var i = handshakeSent; i < total; i++)
    {
        await channel.Writer.WriteAsync(i);
    }

    channel.Writer.Complete();
});

await Task.WhenAll(workers);
sw.Stop();

var rps = total / sw.Elapsed.TotalSeconds;
Console.WriteLine($"Done in {sw.Elapsed.TotalSeconds:F2}s | sent={total} | failures={failures} | avg rps={rps:F1}");

static async Task<TrackingEventResponse> SendEventAsync(HttpClient client, JsonSerializerOptions jsonOpts, Guid sessionId, TrackingEventRequest payload)
{
    if (sessionId == Guid.Empty)
    {
        // First try the no-session route (API should create session)
        using var first = await client.PostAsJsonAsync("entities/events", payload, jsonOpts);
        if (first.IsSuccessStatusCode)
        {
            var body = await first.Content.ReadFromJsonAsync<TrackingEventResponse>(jsonOpts);
            return body ?? throw new InvalidOperationException("Missing event payload");
        }

        // Fallback: force a session id route if the server does not allow /entities/events
        var fallbackSessionId = Guid.NewGuid();
        payload.SessionId = fallbackSessionId;
        using var second = await client.PostAsJsonAsync($"entities/{fallbackSessionId}/events", payload, jsonOpts);
        second.EnsureSuccessStatusCode();
        var fallbackBody = await second.Content.ReadFromJsonAsync<TrackingEventResponse>(jsonOpts);
        return fallbackBody ?? throw new InvalidOperationException("Missing event payload");
    }

    using var resp = await client.PostAsJsonAsync($"entities/{sessionId}/events", payload, jsonOpts);
    resp.EnsureSuccessStatusCode();
    var bodyFinal = await resp.Content.ReadFromJsonAsync<TrackingEventResponse>(jsonOpts);
    return bodyFinal ?? throw new InvalidOperationException("Missing event payload");
}

static string CreateFakeJwtWithCid(Guid companyId, Guid employeeId, string secretKey)
{
    static string B64Url(ReadOnlySpan<byte> bytes) => Convert.ToBase64String(bytes)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    static string B64UrlJson(string json) => B64Url(System.Text.Encoding.UTF8.GetBytes(json));

    var header = B64UrlJson("""{""alg"":""HS256"",""typ"":""JWT""}""");
    var payload = B64UrlJson($"{{\"cid\":\"{companyId}\",\"eid\":\"{employeeId}\"}}");

    var signingInput = $"{header}.{payload}";
    using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey));
    var signature = B64Url(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signingInput)));

    return $"{signingInput}.{signature}"; // signed token satisfies JwtSecurityTokenHandler
}

public sealed class TrackingEventRequest
{
    public Guid SessionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Refer { get; set; } = string.Empty;
    public int ExposeTime { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string BrowserVersion { get; set; } = string.Empty;
    public string NetworkType { get; set; } = string.Empty;
    public string NetworkEffectiveType { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public int ViewportHeight { get; set; }
    public string Properties { get; set; } = "{}";
}

public sealed class TrackingEventResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("entity_id")]
    public Guid EntityId { get; set; }

    [JsonPropertyName("session_id")]
    public Guid SessionId { get; set; }
}
