using System.Text.Json.Serialization;

namespace Mayo.Platform.Tracklix.WebAPI.Models
{
    public class Event
    {
        public string ProductId { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string ScreenId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public EventMetadata Metadata { get; set; } = new EventMetadata();
        public DeviceInfo DeviceInfo { get; set; } = new DeviceInfo();
        public EventFlags Flags { get; set; } = new EventFlags();
    }

    public class EventFlags
    {
        public bool UnknownEventType { get; set; } = false;
        public bool UnknownDeviceType { get; set; } = false;
        public bool ExtraFields { get; set; } = false;
    }
}