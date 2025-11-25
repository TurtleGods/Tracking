using Mayo.Platform.Tracklix.WebAPI.Models;

namespace Mayo.Platform.Tracklix.WebAPI.Services
{
    public class EventCursorHandler
    {
        // Format: {timestamp}|{companyId}|{employeeId}|{deviceType}|{deviceId}
        public string? BuildNextCursor(List<Event> events)
        {
            if (events == null || !events.Any())
                return null;

            var lastEvent = events.Last();
            return $"{lastEvent.Timestamp}|{lastEvent.CompanyId}|{lastEvent.EmployeeId}|{lastEvent.DeviceInfo.DeviceType}|{lastEvent.DeviceId}";
        }

        public (long timestamp, string companyId, string employeeId, string deviceType, string deviceId)? ParseCursor(string? cursor)
        {
            if (string.IsNullOrEmpty(cursor))
                return null;

            var cursorParts = cursor.Split('|');
            if (cursorParts.Length != 5)
            {
                // Invalid cursor format, return null to indicate fallback to beginning
                return null;
            }

            if (!long.TryParse(cursorParts[0], out var timestamp))
            {
                // Invalid timestamp format, return null to indicate fallback to beginning
                return null;
            }

            var companyId = cursorParts[1];
            var employeeId = cursorParts[2];
            var deviceType = cursorParts[3];
            var deviceId = cursorParts[4];

            return (timestamp, companyId, employeeId, deviceType, deviceId);
        }
    }
}