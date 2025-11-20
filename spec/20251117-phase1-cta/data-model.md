# Data Model

## Event Schema
The data structure for storing user behavior events in the system.

```json
{
  "productId": "string",
  "companyId": "string",
  "employeeId": "string",
  "sessionId": "string",
  "screenId": "string",
  "eventType": "string",
  "timestamp": "number (epoch milliseconds)",
  "deviceId": "string (Android | IOS | Browser)",
  "eventId": "string (timestamp-CompanyId-EmployeeId-DeviceId format)",
  "metadata": {
    "view": "string (required for enter_screen)",
    "componentId": "string (required for click)",
    "duration": "number (required for leave_screen)"
  },
  "deviceInfo": {
    "deviceType": "string (Android | IOS | Browser)",
    "os": "string (Android | IOS | Browser)"
  },
  "flags": {
    "duplicate_eventId": "boolean",
    "unknown_eventType": "boolean",
    "extra_fields": "boolean"
  }
}
```

## Event Storage Model
The internal representation of events stored in the system.

### In-Memory Storage
- Append-only list of raw events
- Ordered by `eventId` (timestamp-CompanyId-EmployeeId-DeviceId format)
- Preserves original structure including all flags
- Cleared on system restart (POC requirement)

## Batch Request Schema
The structure for submitting multiple events in a single request.

```json
{
  "events": [
    // Array of up to 10 Event objects
  ]
}
```

## Query Response Schema
The structure for querying and retrieving events.

```json
{
  "events": [
    // Array of raw Event objects as stored
  ],
  "next_cursor": "string ({timestamp}|{companyId}|{employeeId}|{deviceId})",
  "size": "number"
}
```

## Batch Response Schema
The structure for responses to batch event submissions.

```json
{
  "accepted": [
    {
      "eventId": "string"
    }
  ],
  "rejected": [
    {
      "eventId": "string",
      "error_code": "string (uppercase with underscores)",
      "message": "string"
    }
  ]
}
```

## Cursor Format
- Format: `{timestamp}|{companyId}|{employeeId}|{deviceId}`
- Timestamp: Epoch milliseconds from the last event in the current page
- CompanyId: The company identifier from the last event
- EmployeeId: The employee identifier from the last event
- DeviceId: The device identifier from the last event
- Invalid cursors automatically fallback to the beginning