using Mayo.Platform.Tracklix.WebAPI.Models;

namespace Mayo.Platform.Tracklix.WebAPI.Services
{
    public interface IEventStore
    {
        Task AppendEventsAsync(List<Event> events);
        Task<List<Event>> GetEventsAsync(string? cursor = null, int size = 100);
        Task<List<Event>> GetEventsByCompanyAsync(string companyId, string? cursor = null, int size = 100);
        Task<bool> CheckDuplicateEventAsync(string eventId);
    }
}