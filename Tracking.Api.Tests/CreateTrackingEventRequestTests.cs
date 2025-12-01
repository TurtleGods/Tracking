using Tracking.Api.Requests;

namespace Tracking.Api.Tests;

public sealed class CreateTrackingEventRequestTests
{
    [Fact]
    public void ToTrackingEvent_MapsAllFields()
    {
        var timestamp = DateTime.UtcNow;
        var request = new CreateTrackingEventRequest
        {
            EventType = "type",
            EventName = "name",
            Production = "PT",
            PageName = "page",
            ComponentName = "component",
            Timestamp = timestamp,
            Refer = "refer",
            ExposeTime = 42,
            DeviceType = "device",
            OsVersion = "os",
            BrowserVersion = "browser",
            PageUrl = "url",
            PageTitle = "title",
            Properties = "{\"k\":\"v\"}"
        };

        var entityId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var result = request.ToTrackingEvent(entityId, sessionId, employeeId, companyId);

        Assert.Equal(entityId, result.EntityId);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(employeeId, result.EmployeeId);
        Assert.Equal(companyId, result.CompanyId);
        Assert.Equal(request.EventType, result.EventType);
        Assert.Equal(request.EventName, result.EventName);
        Assert.Equal(request.PageName, result.PageName);
        Assert.Equal(request.ComponentName, result.ComponentName);
        Assert.Equal(timestamp, result.Timestamp);
        Assert.Equal(request.Refer, result.Refer);
        Assert.Equal(request.ExposeTime, result.ExposeTime);
        Assert.Equal(request.DeviceType, result.DeviceType);
        Assert.Equal(request.OsVersion, result.OsVersion);
        Assert.Equal(request.BrowserVersion, result.BrowserVersion);
        Assert.Equal(request.PageUrl, result.PageUrl);
        Assert.Equal(request.PageTitle, result.PageTitle);
        Assert.Equal(request.Properties, result.Properties);
    }

    [Fact]
    public void ToTrackingEvent_DefaultsTimestampWhenNull()
    {
        var request = new CreateTrackingEventRequest
        {
            EventType = "type",
            EventName = "name",
            Production = "PT"
        };

        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = request.ToTrackingEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(result.Timestamp, before, after);
    }
}
