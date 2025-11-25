using System.Collections.Concurrent;
using Mayo.Platform.Tracklix.WebAPI.Models;

namespace Mayo.Platform.Tracklix.WebAPI.Services
{
    public class InMemoryEventStore : IEventStore
    {
        private readonly ConcurrentDictionary<string, Event> _events = new();
        private readonly object _lock = new object();

        public Task AppendEventsAsync(List<Event> events)
        {
            if (events == null || !events.Any())
                return Task.CompletedTask;

            lock (_lock)
            {
                foreach (var @event in events)
                {
                    _events[@event.EventId] = @event;
                }
            }

            return Task.CompletedTask;
        }

        public Task<List<Event>> GetEventsAsync(string? cursor = null, int size = 100)
        {
            var allEvents = new List<Event>(_events.Values);
            
            // Sort by timestamp-CompanyId-EmployeeId-DeviceType-DeviceId (as per spec)
            var sortedEvents = allEvents
                .OrderBy(e => e.Timestamp)
                .ThenBy(e => e.CompanyId)
                .ThenBy(e => e.EmployeeId)
                .ThenBy(e => e.DeviceInfo.DeviceType)
                .ThenBy(e => e.DeviceId)
                .ToList();

            var result = ApplyCursorAndSize(sortedEvents, cursor, size);
            return Task.FromResult(result);
        }

        public Task<List<Event>> GetEventsByCompanyAsync(string companyId, string? cursor = null, int size = 100)
        {
            var companyEvents = _events.Values
                .Where(e => e.CompanyId == companyId)
                .ToList();
            
            // Sort by timestamp-CompanyId-EmployeeId-DeviceType-DeviceId (as per spec)
            var sortedEvents = companyEvents
                .OrderBy(e => e.Timestamp)
                .ThenBy(e => e.CompanyId)
                .ThenBy(e => e.EmployeeId)
                .ThenBy(e => e.DeviceInfo.DeviceType)
                .ThenBy(e => e.DeviceId)
                .ToList();

            var result = ApplyCursorAndSize(sortedEvents, cursor, size);
            return Task.FromResult(result);
        }

        public Task<bool> CheckDuplicateEventAsync(string eventId)
        {
            return Task.FromResult(_events.ContainsKey(eventId));
        }

        private List<Event> ApplyCursorAndSize(List<Event> events, string? cursor, int size)
        {
            if (string.IsNullOrEmpty(cursor))
            {
                return events.Take(size).ToList();
            }

            // Parse cursor - format: {timestamp}|{companyId}|{employeeId}|{deviceType}|{deviceId}
            var cursorParts = cursor.Split('|');
            if (cursorParts.Length != 5)
            {
                // Invalid cursor format, return first 'size' events
                return events.Take(size).ToList();
            }

            if (!long.TryParse(cursorParts[0], out var cursorTimestamp))
            {
                // Invalid timestamp, return first 'size' events
                return events.Take(size).ToList();
            }

            var cursorCompanyId = cursorParts[1];
            var cursorEmployeeId = cursorParts[2];
            var cursorDeviceType = cursorParts[3];
            var cursorDeviceId = cursorParts[4];

            // Find events that come after the cursor
            var result = new List<Event>();

            foreach (var evt in events)
            {
                // Compare using the tuple order: (timestamp, companyId, employeeId, deviceType, deviceId)
                var comparison = CompareEventToCursor(
                    evt, 
                    cursorTimestamp, 
                    cursorCompanyId, 
                    cursorEmployeeId, 
                    cursorDeviceType, 
                    cursorDeviceId
                );

                if (comparison > 0) // Event comes after cursor
                {
                    result.Add(evt);
                    
                    if (result.Count >= size)
                        break;
                }
            }

            return result;
        }

        private int CompareEventToCursor(
            Event evt, 
            long cursorTimestamp, 
            string cursorCompanyId, 
            string cursorEmployeeId, 
            string cursorDeviceType, 
            string cursorDeviceId)
        {
            // Compare by timestamp first
            var timestampComparison = evt.Timestamp.CompareTo(cursorTimestamp);
            if (timestampComparison != 0)
                return timestampComparison;

            // Then by companyId
            var companyComparison = string.Compare(evt.CompanyId, cursorCompanyId, StringComparison.Ordinal);
            if (companyComparison != 0)
                return companyComparison;

            // Then by employeeId
            var employeeComparison = string.Compare(evt.EmployeeId, cursorEmployeeId, StringComparison.Ordinal);
            if (employeeComparison != 0)
                return employeeComparison;

            // Then by deviceType
            var deviceTypeComparison = string.Compare(evt.DeviceInfo.DeviceType, cursorDeviceType, StringComparison.Ordinal);
            if (deviceTypeComparison != 0)
                return deviceTypeComparison;

            // Finally by deviceId
            return string.Compare(evt.DeviceId, cursorDeviceId, StringComparison.Ordinal);
        }
    }
}