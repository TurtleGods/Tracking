# Domain Model

## Event
The core domain object representing a user action or behavior event.

### Properties
- `productId`: Unique identifier for the product
- `companyId`: Unique identifier for the company (for multi-tenancy)
- `employeeId`: Unique identifier for the employee/user
- `sessionId`: Unique identifier for the user's session
- `screenId`: Identifier for the screen/view where the event occurred
- `eventType`: Type of event (e.g., enter_screen, click, leave_screen)
- `timestamp`: Epoch milliseconds when the event occurred
- `deviceId`: Device identifier (unique device ID rather than device type enum)
- `eventId`: Unique identifier for the event (timestamp-CompanyId-EmployeeId-DeviceType-DeviceId format)
- `metadata`: Additional data specific to the event type
- `deviceInfo`: Information about the device where the event occurred
- `flags`: Additional flags for the event (e.g., unknown_eventType, extra_fields)

## EventMetadata
Metadata associated with different types of events.

### Properties
- `view`: Required for `enter_screen` events, identifies the view being entered
- `componentId`: Required for `click` events, identifies the component that was clicked
- `duration`: Required for `leave_screen` events, represents the duration of stay in seconds or milliseconds

## DeviceInfo
Information about the device where the event occurred.

### Properties
- `deviceType`: Type of device (`Android` | `IOS` | `Browser`)
- `os`: Operating system (`Android` | `IOS` | `Browser`)

## EventResponseModels
Models for the API response containing accepted and rejected events.

### Properties
- `accepted`: Array of events that were successfully processed
  - `eventId`: The event ID
- `rejected`: Array of events that were rejected
  - `eventId`: The event ID
  - `error_code`: Error code in uppercase with underscores
  - `message`: Human-readable error message