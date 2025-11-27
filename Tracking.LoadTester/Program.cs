using System.Diagnostics;
using System.Net.Http.Json;                                                                                                                                                          
using System.Text.Json;                                                                                                                                                              
using System.Text.Json.Serialization;                                                                                                                                                
using System.Threading.Channels;                                                                                                                                                     
                                                                                                                                                                                    
var baseUrl     = Environment.GetEnvironmentVariable("TARGET_BASE") ?? "http://localhost:8080";                                                                                      
var total       = int.TryParse(Environment.GetEnvironmentVariable("TOTAL_EVENTS") ?? "100000", out var t) ? t : 100000;                                                              
var concurrency = int.TryParse(Environment.GetEnvironmentVariable("CONCURRENCY") ?? "64", out var c) ? c : 64;                                                                       
var progress    = int.TryParse(Environment.GetEnvironmentVariable("PROGRESS_STEP") ?? "1000", out var p) && p > 0 ? p : 1000;                                                        
var entityIdEnv = Environment.GetEnvironmentVariable("ENTITY_ID");                                                                                                                   
var sessionIdEnv = Environment.GetEnvironmentVariable("SESSION_ID");                                                                                                                 
var companyIdEnv = Environment.GetEnvironmentVariable("COMPANY_ID");                                                                                                                 
var progressLock = new object();                                                                                                                                                     
                                                                                                                                                                                    
var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web); // camelCase for requests                                                                                      
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };                                                                                                                
                                                                                                                                                                                    
Console.WriteLine($"Base={baseUrl} total={total} concurrency={concurrency}");                                                                                                        
                                                                                                                                                                                    
var entityId = Guid.TryParse(entityIdEnv, out var existingEntity)                                                                                                                    
    ? existingEntity                                                                                                                                                                 
    : await CreateEntityAsync(client, jsonOpts, companyIdEnv);                                                                                                                       
var sessionId = Guid.TryParse(sessionIdEnv, out var existingSession)                                                                                                                 
    ? existingSession                                                                                                                                                                
    : await CreateSessionAsync(client, jsonOpts, entityId, total);                                                                                                                   
                                                                                                                                                                                    
Console.WriteLine($"Using entity={entityId} session={sessionId}");                                                                                                                   
                                                                                                                                                                                    
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
            UserId = Random.Shared.Next(1, 10_000_000),                                                                                                                              
            CompanyId = Random.Shared.Next(1, 10_000),
            DeviceType = "web",                                                                                                                                                      
            OsVersion = "macOS 15",                                                                                                                                                  
            BrowserVersion = "Chrome 130",                                                                                                                                           
            NetworkType = "wifi",                                                                                                                                                    
            NetworkEffectiveType = "4g",                                                                                                                                             
            PageUrl = "https://app.local/dashboard",                                                                                                                                 
            PageTitle = "Dashboard",                                                                                                                                                 
            ViewportHeight = 900,                                                                                                                                                    
            Properties = """{"variant":"A","cta":"signup"}"""                                                                                                                        
        };                                                                                                                                                                           
                                                                                                                                                                                    
        try                                                                                                                                                                          
        {                                                                                                                                                                     
            using var resp = await client.PostAsJsonAsync($"entities/{entityId}/events", payload, jsonOpts);                                                                         
            if (!resp.IsSuccessStatusCode) Interlocked.Increment(ref failures);                                                                                                      
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
        await channel.Writer.WriteAsync(i);                                                                                                                                          
    channel.Writer.Complete();                                                                                                                                                       
});                                                                                                                                                                                  
                                                                                                                                                                                    
await Task.WhenAll(workers);                                                                                                                                                         
sw.Stop();                                                                                                                                                                           
                                                                                                                                                                                    
var rps = total / sw.Elapsed.TotalSeconds;                                                                                                                                           
Console.WriteLine($"Done in {sw.Elapsed.TotalSeconds:F2}s | sent={total} | failures={failures} | avg rps={rps:F1}");                                                                 
                                                                                                                                                                                    
static async Task<Guid> CreateEntityAsync(HttpClient client, JsonSerializerOptions jsonOpts, string? companyIdEnv)                                                                    
{                                                                                                                                                                                    
    var companyId = Guid.TryParse(companyIdEnv, out var parsedCompany) ? parsedCompany : Guid.NewGuid();                                                                             
    var req = new                                                                                                                                                                    
    {                                                                                                                                                                                
        creatorId = 999,
        companyId,                                                                                                                                                       
        creatorEmail = "pt@example.com",                                                                                                                                             
        title = "FD",
        production="FD",                                                                                                                                                                
        panels = "{}",                                                                                                                                                               
        collaborators = "[]",                                                                                                                                                        
        visibility = "private",                                                                                                                                                      
        isShared = false                                                                                                                                                             
    };                                                                                                                                                                               
                                                                                                                                                                                    
    using var resp = await client.PostAsJsonAsync("entities", req, jsonOpts);                                                                                                        
    resp.EnsureSuccessStatusCode();                                                                                                                                                  
    var body = await resp.Content.ReadFromJsonAsync<EntityResponse>();                                                                                                               
    return body?.EntityId ?? throw new InvalidOperationException("Missing entity_id");                                                                                               
}                                                                                                                                                                                    
                                                                                                                                                                                    
static async Task<Guid> CreateSessionAsync(HttpClient client, JsonSerializerOptions jsonOpts, Guid entityId, int totalEvents)                                                        
{                                                                                                                                                                                    
    var req = new                                                                                                                                                                    
    {                                                                                                                                                                                
        userId = 1234L,                                                                                                                                                              
        startedAt = DateTime.UtcNow,                                                                                                                                                 
        lastActivityAt = DateTime.UtcNow,                                                                                                                                            
        endedAt = DateTime.UtcNow,                                                                                                                                                   
        totalEvents = totalEvents,                                                                                                                                                   
        totalViews = totalEvents / 2,                                                                                                                                                
        totalClicks = totalEvents / 3,                                                                                                                                               
        totalExposes = totalEvents / 4,                                                                                                                                              
        totalDisappears = totalEvents / 5,                                                                                                                                           
        deviceType = "web",                                                                                                                                                          
        deviceModel = "browser",                                                                                                                                                     
        entryPage = "/landing",                                                                                                                                                      
        exitPage = "/exit"                                                                                                                                                           
    };                                                                                                                                                                               
                                                                                                                                                                                    
    using var resp = await client.PostAsJsonAsync($"entities/{entityId}/sessions", req, jsonOpts);                                                                                   
    resp.EnsureSuccessStatusCode();                                                                                                                                                  
    var body = await resp.Content.ReadFromJsonAsync<SessionResponse>();                                                                                                              
    return body?.Id ?? throw new InvalidOperationException("Missing session id");                                                                                                    
}                                                                                                                                                                                    
                                                                                                                                                                                    
public sealed record EntityResponse([property: JsonPropertyName("entity_id")] Guid EntityId);                                                                                        
public sealed record SessionResponse([property: JsonPropertyName("id")] Guid Id);                                                                                                    
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
    public long UserId { get; set; }                                                                                                                                                 
    public long CompanyId { get; set; }                                                                                                                                              
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
