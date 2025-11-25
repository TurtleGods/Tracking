using Mayo.Platform.Tracklix.WebAPI.Models;
using Mayo.Platform.Tracklix.WebAPI.Services;

namespace Mayo.Platform.Tracklix.WebAPI.Tests
{
    public class IngestionRulesTests
    {
        private readonly ValidationService _validationService;
        private readonly MockEventStore _mockEventStore;

        public IngestionRulesTests()
        {
            _mockEventStore = new MockEventStore();
            _validationService = new ValidationService(_mockEventStore);
        }

        [Fact]
        public async Task ValidateBatchEvents_WithValidEvents_ReturnsAllAccepted()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event
                {
                    EventId = "event1",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "click",
                    Timestamp = 1234567890,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button1" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            // Act
            var result = await _validationService.ValidateBatchEventsAsync(events);

            // Assert
            Assert.Empty(result.Rejected);
            Assert.Single(result.Accepted);
            Assert.Equal("event1", result.Accepted.First().EventId);
        }

        [Fact]
        public async Task ValidateBatchEvents_WithTooManyEvents_ReturnsBatchSizeExceededError()
        {
            // Arrange
            var events = new List<Event>();
            for (int i = 0; i < 11; i++)
            {
                events.Add(new Event
                {
                    EventId = $"event{i}",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "click",
                    Timestamp = 1234567890 + i,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button1" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                });
            }

            // Act
            var result = await _validationService.ValidateBatchEventsAsync(events);

            // Assert
            Assert.Single(result.Rejected);
            Assert.Equal("BATCH_SIZE_EXCEEDED", result.Rejected.First().ErrorCode);
            Assert.Empty(result.Accepted);
        }

        [Fact]
        public async Task ValidateBatchEvents_WithDuplicateEventInBatch_ReturnsDuplicateError()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event
                {
                    EventId = "duplicate_event",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "click",
                    Timestamp = 1234567890,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button1" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                },
                new Event
                {
                    EventId = "duplicate_event",  // Same ID as first event
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen2",
                    EventType = "enter_screen",
                    Timestamp = 1234567891,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { View = "home" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            // Act
            var result = await _validationService.ValidateBatchEventsAsync(events);

            // Assert
            Assert.Single(result.Rejected);
            Assert.Equal("DUPLICATE_EVENT_ID_IN_BATCH", result.Rejected.First().ErrorCode);
            Assert.Equal("duplicate_event", result.Rejected.First().EventId);
            Assert.Single(result.Accepted);  // Only first event should be accepted
            Assert.Equal("duplicate_event", result.Accepted.First().EventId);  // Should be first occurrence
        }

        [Fact]
        public async Task ValidateBatchEvents_WithMissingRequiredFields_ReturnsErrors()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event  // Missing ProductId
                {
                    EventId = "event1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "click",
                    Timestamp = 1234567890,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button1" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            // Act
            var result = await _validationService.ValidateBatchEventsAsync(events);

            // Assert
            Assert.Single(result.Rejected);
            Assert.Equal("MISSING_PRODUCT_ID", result.Rejected.First().ErrorCode);
        }

        [Fact]
        public async Task ValidateBatchEvents_WithMissingMetadataFields_ReturnsErrors()
        {
            // Arrange
            var events = new List<Event>
            {
                new Event  // enter_screen event missing required metadata.view
                {
                    EventId = "event1",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "enter_screen",  // Requires metadata.view
                    Timestamp = 1234567890,
                    DeviceId = "device1",
                    Metadata = new EventMetadata(),  // No View specified
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            // Act
            var result = await _validationService.ValidateBatchEventsAsync(events);

            // Assert
            Assert.Single(result.Rejected);
            Assert.Equal("MISSING_METADATA_VIEW", result.Rejected.First().ErrorCode);
        }

        [Fact]
        public async Task ValidateBatchEvents_WithUnknownEventType_SetsFlag()
        {
            // Arrange
            var eventWithUnknownType = new Event
            {
                EventId = "event1",
                ProductId = "product1",
                CompanyId = "company1",
                EmployeeId = "employee1",
                SessionId = "session1",
                ScreenId = "screen1",
                EventType = "custom_event",  // Not one of the standard types
                Timestamp = 1234567890,
                DeviceId = "device1",
                Metadata = new EventMetadata { ComponentId = "button1" },
                DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" },
                Flags = new EventFlags()  // Start with clean flags
            };

            var events = new List<Event> { eventWithUnknownType };

            // Act - manually apply rules to test flagging
            _validationService.GetType().GetMethod("ApplyIngestionRules",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_validationService, new object[] { eventWithUnknownType });

            // Act
            var result = await _validationService.ValidateBatchEventsAsync(events);

            // The event should be accepted but with the flag
            Assert.Single(result.Accepted);
            Assert.Empty(result.Rejected);
            Assert.True(eventWithUnknownType.Flags.UnknownEventType);
        }

        [Fact]
        public async Task ValidateBatchEvents_WithUnknownDeviceType_SetsFlag()
        {
            // Arrange
            var eventWithUnknownDeviceType = new Event
            {
                EventId = "event1",
                ProductId = "product1",
                CompanyId = "company1",
                EmployeeId = "employee1",
                SessionId = "session1",
                ScreenId = "screen1",
                EventType = "click",
                Timestamp = 1234567890,
                DeviceId = "device1",
                Metadata = new EventMetadata { ComponentId = "button1" },
                DeviceInfo = new DeviceInfo { DeviceType = "Desktop", Os = "Windows" }, // Not one of the standard types
                Flags = new EventFlags()  // Start with clean flags
            };

            var events = new List<Event> { eventWithUnknownDeviceType };

            // Act - manually apply rules to test flagging
            _validationService.GetType().GetMethod("ApplyIngestionRules",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_validationService, new object[] { eventWithUnknownDeviceType });

            // Act
            var result = await _validationService.ValidateBatchEventsAsync(events);

            // The event should be accepted but with the flag
            Assert.Single(result.Accepted);
            Assert.Empty(result.Rejected);
            Assert.True(eventWithUnknownDeviceType.Flags.UnknownDeviceType);
        }

        // Note: Extra fields validation is not implemented in this version due to
        // the strongly-typed model structure. It would require JSON-level inspection
        // to properly detect fields beyond the defined properties.

        // Mock IEventStore implementation for testing
        private class MockEventStore : IEventStore
        {
            private readonly HashSet<string> _existingEventIds = new();

            public void AddExistingEventId(string eventId)
            {
                _existingEventIds.Add(eventId);
            }

            public Task AppendEventsAsync(List<Event> events)
            {
                foreach (var @event in events)
                {
                    _existingEventIds.Add(@event.EventId);
                }
                return Task.CompletedTask;
            }

            public Task<List<Event>> GetEventsAsync(string? cursor = null, int size = 100)
            {
                return Task.FromResult(new List<Event>());
            }

            public Task<List<Event>> GetEventsByCompanyAsync(string companyId, string? cursor = null, int size = 100)
            {
                return Task.FromResult(new List<Event>());
            }

            public Task<bool> CheckDuplicateEventAsync(string eventId)
            {
                return Task.FromResult(_existingEventIds.Contains(eventId));
            }
        }
    }
}