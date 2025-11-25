namespace Mayo.Platform.Tracklix.WebAPI.Models
{
    public class BatchEventRequest
    {
        public List<Event> Events { get; set; } = new List<Event>();
    }

    public class BatchEventResponse
    {
        public List<AcceptedEvent> Accepted { get; set; } = new List<AcceptedEvent>();
        public List<RejectedEvent> Rejected { get; set; } = new List<RejectedEvent>();
    }

    public class AcceptedEvent
    {
        public string EventId { get; set; } = string.Empty;
    }

    public class RejectedEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class QueryEventsResponse
    {
        public List<Event> Events { get; set; } = new List<Event>();
        public string? NextCursor { get; set; }
        public int Size { get; set; }
    }
}