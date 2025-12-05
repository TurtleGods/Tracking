using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.IdentityModel.Tokens;

var baseUrl = Environment.GetEnvironmentVariable("TARGET_BASE") ?? "http://localhost:8080";
var total = int.TryParse(Environment.GetEnvironmentVariable("TOTAL_EVENTS") ?? "300000", out var t) ? t : 100000;
var concurrency = int.TryParse(Environment.GetEnvironmentVariable("CONCURRENCY") ?? "500", out var c) ? c : 64;
var progress = int.TryParse(Environment.GetEnvironmentVariable("PROGRESS_STEP") ?? "1000", out var p) && p > 0 ? p : 1000;

var progressLock = new object();
var tokenCache = new ConcurrentDictionary<(Guid companyId, Guid employeeId), string>();
var eventTemplates = new[]
{
    new EventTemplate("View", "View_Demo_TestPage", "Demo_Page", "page", "https://app.local/demo", "Demo", "https://app.local/dashboard"),
    new EventTemplate("Click", "Click_Demo_ManualClickButton", "Demo_Page", "manual_button", "https://app.local/demo", "Demo", "https://app.local/dashboard"),
    new EventTemplate("Expose", "Expose_TestPage_ExposeTestCard", "Test_Page", "expose_test_card", "https://app.local/test", "Test", "https://app.local/demo"),
    new EventTemplate("Disappear", "Disappear_TestPage_DisappearTestCard", "Test_Page", "disappear_test_card", "https://app.local/test", "Test", "https://app.local/test")
};
var deviceTypes = new[] { "Web", "phone", "ipad" };
var browserVersions = new[] { "Chrome", "safari", "Firefox", "Edge" };
var productions = new[] { "PT", "FD", "PY" };
var companyIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();
var employeesByCompany = BuildEmployeesForCompanies(companyIds, 100);
var userSessions = employeesByCompany
    .SelectMany(kvp => kvp.Value.Select(emp => new UserSession(kvp.Key, emp, Guid.NewGuid())))
    .ToArray();

var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web); // camelCase for requests
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
Console.WriteLine($"Base={baseUrl} total={total} concurrency={concurrency}");
Console.WriteLine($"Companies={companyIds.Length} employees/company={employeesByCompany.First().Value.Length}");
Console.WriteLine($"Pre-created sessions={userSessions.Length}");

var channel = Channel.CreateBounded<int>(concurrency * 4);
var sw = Stopwatch.StartNew();
long failures = 0;
long sent = 0;

var workers = Enumerable.Range(0, concurrency).Select(async _ =>
{
    await foreach (var i in channel.Reader.ReadAllAsync())
    {
        var userSession = userSessions[i % userSessions.Length];
        var companyId = userSession.CompanyId;
        var employeeId = userSession.EmployeeId;
        var sessionId = userSession.SessionId;
        var template = eventTemplates[i % eventTemplates.Length];
        var deviceType = deviceTypes[Random.Shared.Next(deviceTypes.Length)];
        var osVersion = PickOsVersion(deviceType);
        var browserVersion = browserVersions[Random.Shared.Next(browserVersions.Length)];
        var production = productions[i % productions.Length];
        var effectiveExpose = template.EventType is "View" or "Expose" ? Random.Shared.Next(500, 5000) : 0;
        var properties = JsonSerializer.Serialize(new
        {
            variant = Random.Shared.Next(0, 2) == 0 ? "A" : "B",
            flow = template.PageName,
            placement = template.ComponentName,
            sequence = i
        });

        var payload = new TrackingEventRequest
        {
            SessionId = sessionId,
            Production = production,
            EventType = template.EventType,
            EventName = template.EventName,
            PageName = template.PageName,
            ComponentName = template.ComponentName,
            Timestamp = DateTime.UtcNow,
            Refer = template.Refer,
            ExposeTime = effectiveExpose,
            EmployeeId = employeeId,
            CompanyId = companyId,
            DeviceType = deviceType,
            OsVersion = osVersion,
            BrowserVersion = browserVersion,
            PageUrl = template.PageUrl,
            PageTitle = template.PageTitle,
            ViewportHeight = deviceType == "Web" ? 1080 : 780,
            Properties = properties
        };

        try
        {
            var token = GetOrCreateToken(tokenCache, companyId, employeeId);
            var ok = await SendEventAsync(client, jsonOpts, sessionId, payload, token);
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
                }            }
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

static async Task<bool> SendEventAsync(HttpClient client, JsonSerializerOptions jsonOpts, Guid sessionId, TrackingEventRequest payload, string cookie)
{
    using var request = new HttpRequestMessage(HttpMethod.Post, $"entities/{sessionId}/events")
    {
        Content = JsonContent.Create(payload, options: jsonOpts)
    };
    request.Headers.Add("Cookie", $"__ModuleSessionCookie={cookie}");

    using var resp = await client.SendAsync(request);
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

static string GetOrCreateToken(ConcurrentDictionary<(Guid companyId, Guid employeeId), string> cache, Guid companyId, Guid employeeId)
{
    return cache.GetOrAdd((companyId, employeeId), _ => CreateFakeJwt(companyId, employeeId));
}

static Dictionary<Guid, Guid[]> BuildEmployeesForCompanies(IEnumerable<Guid> companies, int employeesPerCompany)
{
    var allEmployees = new HashSet<Guid>();
    var result = new Dictionary<Guid, Guid[]>();

    foreach (var company in companies)
    {
        var employees = new List<Guid>(employeesPerCompany);
        while (employees.Count < employeesPerCompany)
        {
            var candidate = Guid.NewGuid();
            if (allEmployees.Add(candidate))
            {
                employees.Add(candidate);
            }
        }

        result[company] = employees.ToArray();
    }

    return result;
}

static string PickOsVersion(string deviceType) => deviceType switch
{
    "Web" => "MacOs",
    "ipad" => "iOs",
    "phone" => Random.Shared.Next(0, 2) == 0 ? "iOs" : "Android",
    _ => "MacOs"
};

public sealed record EventTemplate(
    string EventType,
    string EventName,
    string PageName,
    string ComponentName,
    string PageUrl,
    string PageTitle,
    string Refer
);

public sealed record UserSession(Guid CompanyId, Guid EmployeeId, Guid SessionId);

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
