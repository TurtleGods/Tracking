using System.Text.Json.Serialization;

namespace Mayo.Platform.Tracklix.WebAPI.Models
{
    public class EventMetadata
    {
        public string? View { get; set; }
        public string? ComponentId { get; set; }
        public double? Duration { get; set; }
    }
}