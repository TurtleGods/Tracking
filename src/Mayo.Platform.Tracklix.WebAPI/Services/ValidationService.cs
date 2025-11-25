using System.Text.Json;
using System.Text.Json.Nodes;
using Mayo.Platform.Tracklix.WebAPI.Models;

namespace Mayo.Platform.Tracklix.WebAPI.Services
{
    public class ValidationService
    {
        private readonly IEventStore _eventStore;

        public ValidationService(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<BatchEventResponse> ValidateBatchEventsAsync(List<Event> events)
        {
            var response = new BatchEventResponse();
            var processedEventIds = new HashSet<string>();

            if (events == null || events.Count == 0)
            {
                return response; // Return empty response if no events
            }

            // Check batch size
            if (events.Count > 10)
            {
                response.Rejected.Add(new RejectedEvent
                {
                    EventId = "",
                    ErrorCode = "BATCH_SIZE_EXCEEDED",
                    Message = "Batch size exceeds maximum allowed events of 10"
                });
                return response;
            }

            // Process each event
            foreach (var @event in events)
            {
                // Check for duplicate within the batch
                if (processedEventIds.Contains(@event.EventId))
                {
                    response.Rejected.Add(new RejectedEvent
                    {
                        EventId = @event.EventId,
                        ErrorCode = "DUPLICATE_EVENT_ID_IN_BATCH",
                        Message = $"Duplicate eventId in the same batch: {@event.EventId}"
                    });
                    continue; // Skip this duplicate event
                }

                // Add to processed IDs to detect duplicates in this batch
                processedEventIds.Add(@event.EventId);

                // Validate required fields
                var validationErrors = ValidateEventFields(@event);

                if (validationErrors.Any())
                {
                    // Add all validation errors for this event
                    foreach (var error in validationErrors)
                    {
                        response.Rejected.Add(new RejectedEvent
                        {
                            EventId = @event.EventId,
                            ErrorCode = error.ErrorCode,
                            Message = error.Message
                        });
                    }
                    continue; // Skip this invalid event
                }

                // Check for cross-batch duplicates (duplicates with events already in store)
                var isDuplicate = await _eventStore.CheckDuplicateEventAsync(@event.EventId);
                if (isDuplicate)
                {
                    // According to spec: Cross-batch duplicates should be accepted and stored
                    // So we continue with processing and flagging
                }

                // Apply ingestion rules (flagging) for non-rejection conditions
                ApplyIngestionRules(@event);

                // If we got here, the event is valid
                response.Accepted.Add(new AcceptedEvent { EventId = @event.EventId });
            }

            return response;
        }

        private List<(string ErrorCode, string Message)> ValidateEventFields(Event @event)
        {
            var errors = new List<(string, string)>();

            // Check for required fields
            if (string.IsNullOrEmpty(@event.ProductId))
                errors.Add(("MISSING_PRODUCT_ID", "productId is required"));

            if (string.IsNullOrEmpty(@event.CompanyId))
                errors.Add(("MISSING_COMPANY_ID", "companyId is required"));

            if (string.IsNullOrEmpty(@event.EmployeeId))
                errors.Add(("MISSING_EMPLOYEE_ID", "employeeId is required"));

            if (string.IsNullOrEmpty(@event.SessionId))
                errors.Add(("MISSING_SESSION_ID", "sessionId is required"));

            if (string.IsNullOrEmpty(@event.ScreenId))
                errors.Add(("MISSING_SCREEN_ID", "screenId is required"));

            if (string.IsNullOrEmpty(@event.EventType))
                errors.Add(("MISSING_EVENT_TYPE", "eventType is required"));

            if (@event.Timestamp == 0)
                errors.Add(("MISSING_TIMESTAMP", "timestamp is required"));

            if (string.IsNullOrEmpty(@event.DeviceId))
                errors.Add(("MISSING_DEVICE_ID", "deviceId is required"));

            // Validate timestamp format (should be epoch milliseconds)
            if (@event.Timestamp < 0)
                errors.Add(("INVALID_TIMESTAMP_FORMAT", "Invalid timestamp format. Must be epoch milliseconds"));

            // Validate metadata required fields based on eventType
            if (@event.EventType == "enter_screen" && string.IsNullOrEmpty(@event.Metadata.View))
                errors.Add(("MISSING_METADATA_VIEW", "metadata.view required for enter_screen event"));

            if (@event.EventType == "click" && string.IsNullOrEmpty(@event.Metadata.ComponentId))
                errors.Add(("MISSING_METADATA_COMPONENT_ID", "metadata.componentId required for click event"));

            if (@event.EventType == "leave_screen" && !@event.Metadata.Duration.HasValue)
                errors.Add(("MISSING_METADATA_DURATION", "metadata.duration required for leave_screen event"));

            // Validate duration format if present
            if (@event.Metadata.Duration.HasValue && @event.Metadata.Duration < 0)
                errors.Add(("INVALID_DURATION_FORMAT", "Invalid duration format. Must be a number representing milliseconds"));

            return errors;
        }

        private void ApplyIngestionRules(Event @event)
        {
            // Apply unknown eventType flag
            if (!string.IsNullOrEmpty(@event.EventType) &&
                @event.EventType != "enter_screen" &&
                @event.EventType != "click" &&
                @event.EventType != "leave_screen")
            {
                @event.Flags.UnknownEventType = true;
            }

            // Apply unknown deviceType flag
            if (!string.IsNullOrEmpty(@event.DeviceInfo.DeviceType) &&
                @event.DeviceInfo.DeviceType != "Android" &&
                @event.DeviceInfo.DeviceType != "IOS" &&
                @event.DeviceInfo.DeviceType != "Browser")
            {
                @event.Flags.UnknownDeviceType = true;
            }
        }
    }
}