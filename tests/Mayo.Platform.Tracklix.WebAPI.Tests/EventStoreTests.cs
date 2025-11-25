using Mayo.Platform.Tracklix.WebAPI.Models;
using Mayo.Platform.Tracklix.WebAPI.Services;

namespace Mayo.Platform.Tracklix.WebAPI.Tests
{
    public class EventStoreTests
    {
        private readonly IEventStore _eventStore;

        public EventStoreTests()
        {
            _eventStore = new InMemoryEventStore();
        }

        [Fact]
        public async Task AppendEvents_AddsEventsToStore()
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
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            // Act
            await _eventStore.AppendEventsAsync(events);

            // Assert
            var retrievedEvents = await _eventStore.GetEventsAsync();
            Assert.Single(retrievedEvents);
            Assert.Equal("event1", retrievedEvents.First().EventId);
        }

        [Fact]
        public async Task GetEvents_ReturnsAllEvents()
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
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                },
                new Event
                {
                    EventId = "event2",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen2",
                    EventType = "enter_screen",
                    Timestamp = 1234567891,
                    DeviceId = "device1",
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            await _eventStore.AppendEventsAsync(events);

            // Act
            var retrievedEvents = await _eventStore.GetEventsAsync();

            // Assert
            Assert.Equal(2, retrievedEvents.Count);
        }

        [Fact]
        public async Task GetEventsByCompany_ReturnsOnlyCompanyEvents()
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
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                },
                new Event
                {
                    EventId = "event2",
                    ProductId = "product1",
                    CompanyId = "company2",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen2",
                    EventType = "enter_screen",
                    Timestamp = 1234567891,
                    DeviceId = "device1",
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            await _eventStore.AppendEventsAsync(events);

            // Act
            var company1Events = await _eventStore.GetEventsByCompanyAsync("company1");

            // Assert
            Assert.Single(company1Events);
            Assert.Equal("company1", company1Events.First().CompanyId);
        }

        [Fact]
        public async Task CheckDuplicateEvent_ReturnsTrueForExistingEvent()
        {
            // Arrange
            var existingEvent = new Event
            {
                EventId = "existing_event",
                ProductId = "product1",
                CompanyId = "company1",
                EmployeeId = "employee1",
                SessionId = "session1",
                ScreenId = "screen1",
                EventType = "click",
                Timestamp = 1234567890,
                DeviceId = "device1",
                DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
            };

            await _eventStore.AppendEventsAsync(new List<Event> { existingEvent });

            // Act
            var isDuplicate = await _eventStore.CheckDuplicateEventAsync("existing_event");

            // Assert
            Assert.True(isDuplicate);
        }

        [Fact]
        public async Task CheckDuplicateEvent_ReturnsFalseForNonExistingEvent()
        {
            // Act
            var isDuplicate = await _eventStore.CheckDuplicateEventAsync("non_existing_event");

            // Assert
            Assert.False(isDuplicate);
        }
    }
}