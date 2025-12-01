using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var baseUrl = Environment.GetEnvironmentVariable("TARGET_BASE") ?? "http://localhost:8080";
var total = int.TryParse(Environment.GetEnvironmentVariable("TOTAL_EVENTS") ?? "10000", out var t) ? t : 100000;
var concurrency = int.TryParse(Environment.GetEnvironmentVariable("CONCURRENCY") ?? "500", out var c) ? c : 64;
var progress = int.TryParse(Environment.GetEnvironmentVariable("PROGRESS_STEP") ?? "1000", out var p) && p > 0 ? p : 1000;
var sessionIdEnv = Environment.GetEnvironmentVariable("SESSION_ID");

var companyId = Guid.NewGuid();
var employeeId = Guid.NewGuid();
var jwtSecret = Environment.GetEnvironmentVariable("LOAD_TEST_JWT_SECRET") ?? "dev-secret";
var progressLock = new object();

var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web); // camelCase for requests
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
client.DefaultRequestHeaders.Add("Cookie", $"__ModuleSessionCookie={CreateFakeJwt(companyId, employeeId)}");
Console.WriteLine($"Base={baseUrl} total={total} concurrency={concurrency}");

var sessionId = Guid.TryParse(sessionIdEnv, out var parsedSession) ? parsedSession : Guid.NewGuid();
Console.WriteLine($"Using session={sessionId} companyId={companyId}");

var channel = Channel.CreateBounded<int>(concurrency * 4);
var sw = Stopwatch.StartNew();
long failures = 0;
long sent = 0;

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
            Properties = """{""variant"":""A"",""cta"":""signup""}""",
            Production = "PT"
        };

        try
        {
            var ok = await SendEventAsync(client, jsonOpts, sessionId, payload);
            if (!ok) Interlocked.Increment(ref failures);
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
    for (var i = 0; i < total; i++)
    {
        await channel.Writer.WriteAsync(i);
    }

    channel.Writer.Complete();
});

await Task.WhenAll(workers);
sw.Stop();

var rps = total / sw.Elapsed.TotalSeconds;
Console.WriteLine($"Done in {sw.Elapsed.TotalSeconds:F2}s | sent={total} | failures={failures} | avg rps={rps:F1}");

static async Task<bool> SendEventAsync(HttpClient client, JsonSerializerOptions jsonOpts, Guid sessionId, TrackingEventRequest payload)
{
    using var resp = await client.PostAsJsonAsync($"entities/{sessionId}/events", payload, jsonOpts);
    resp.EnsureSuccessStatusCode();
    return true;
}

static string CreateFakeJwt(Guid companyId, Guid employeeId)
{
    var claims = new[]
    {
        new Claim("eid", employeeId.ToString()),
        new Claim("cid", companyId.ToString())
    };

    var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("Harvey secret for load testing and development"));

    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: "Tester",
        audience:"Mayo",
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(60),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

public sealed class TrackingEventRequest
{
    public Guid SessionId { get; set; }
    public string Production { get; set; } = "PT";
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
