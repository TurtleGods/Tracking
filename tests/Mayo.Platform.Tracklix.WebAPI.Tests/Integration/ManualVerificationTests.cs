using Mayo.Platform.Tracklix.WebAPI.Models;
using Mayo.Platform.Tracklix.WebAPI.Services;

namespace Mayo.Platform.Tracklix.WebAPI.Tests.Integration
{
    public class ManualVerificationTests
    {
        private readonly IEventStore _eventStore;
        private readonly ValidationService _validationService;
        private readonly EventCursorHandler _cursorHandler;

        public ManualVerificationTests()
        {
            // Set up services
            _eventStore = new InMemoryEventStore();
            _validationService = new ValidationService(_eventStore);
            _cursorHandler = new EventCursorHandler();
        }

        public async Task VerifyPostEventsBatchPartialSuccess()
        {
            Console.WriteLine("Testing POST /events/batch partial success...");

            // Create a batch with some valid and some invalid events
            var events = new List<Event>
            {
                // Valid event
                new Event
                {
                    EventId = "event1_valid",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "click",
                    Timestamp = 1234567890000,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button1" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                },
                // Invalid event - missing required field
                new Event
                {
                    EventId = "event2_invalid",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "", // Missing required field
                    Timestamp = 1234567891000,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button2" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            // Validate the batch
            var result = await _validationService.ValidateBatchEventsAsync(events);

            // Check partial success: one accepted, one rejected
            Console.WriteLine($"  Accepted events: {result.Accepted.Count}");
            Console.WriteLine($"  Rejected events: {result.Rejected.Count}");
            Console.WriteLine($"  Expected: 1 accepted, 1 rejected");
            
            if (result.Accepted.Count == 1 && result.Rejected.Count == 1)
            {
                Console.WriteLine("  ✓ Partial success working correctly");
            }
            else
            {
                Console.WriteLine("  ✗ Partial success NOT working correctly");
            }
        }

        public async Task VerifyQueryApiOrdering()
        {
            Console.WriteLine("\nTesting query API ordering by timestamp-CompanyId-EmployeeId-DeviceType-DeviceId...");

            // Clear any existing events
            // Note: Our InMemoryEventStore doesn't have a clear method, so we'll just add known events
            
            // Add events in different order to test sorting
            var events = new List<Event>
            {
                new Event
                {
                    EventId = "event3",
                    ProductId = "product1",
                    CompanyId = "company2",
                    EmployeeId = "employee2",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "click",
                    Timestamp = 1234567890002, // This should be last
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button3" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                },
                new Event
                {
                    EventId = "event1",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "click",
                    Timestamp = 1234567890000, // This should be first
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button1" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                },
                new Event
                {
                    EventId = "event2",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "click",
                    Timestamp = 1234567890001, // This should be second
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button2" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            await _eventStore.AppendEventsAsync(events);

            // Query events - they should be returned in sorted order
            var queriedEvents = await _eventStore.GetEventsAsync();

            Console.WriteLine($"  Retrieved {queriedEvents.Count} events");
            for (int i = 0; i < queriedEvents.Count; i++)
            {
                Console.WriteLine($"    Event {i+1}: ID={queriedEvents[i].EventId}, Timestamp={queriedEvents[i].Timestamp}");
            }

            // Check if events are in proper order (by timestamp)
            bool isOrdered = true;
            for (int i = 1; i < queriedEvents.Count; i++)
            {
                if (queriedEvents[i].Timestamp < queriedEvents[i-1].Timestamp)
                {
                    isOrdered = false;
                    break;
                }
            }

            if (isOrdered && queriedEvents[0].EventId == "event1" &&
                queriedEvents[1].EventId == "event2" &&
                queriedEvents[2].EventId == "event3")
            {
                Console.WriteLine("  ✓ Query API ordering working correctly");
            }
            else
            {
                Console.WriteLine("  ✗ Query API ordering NOT working correctly");
            }
        }

        public async Task VerifyEventIdDuplicateHandling()
        {
            Console.WriteLine("\nTesting event ID duplicate handling...");

            // Clear and add some events including duplicates
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
                    Timestamp = 1234567890000,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button1" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                },
                new Event
                {
                    EventId = "duplicate_event", // Same ID as above
                    ProductId = "product2", // Different product
                    CompanyId = "company2", // Different company
                    EmployeeId = "employee2", // Different employee
                    SessionId = "session2", // Different session
                    ScreenId = "screen2", // Different screen
                    EventType = "enter_screen",
                    Timestamp = 1234567890001,
                    DeviceId = "device2", // Different device
                    Metadata = new EventMetadata { View = "home" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Android", Os = "Android" }
                }
            };

            // Test batch validation - should reject the second one in the same batch
            var result = await _validationService.ValidateBatchEventsAsync(events);
            
            Console.WriteLine($"  Batch validation - Accepted: {result.Accepted.Count}, Rejected: {result.Rejected.Count}");
            Console.WriteLine($"  Expected: 1 accepted, 1 rejected (same batch duplicate)");

            if (result.Accepted.Count == 1 && result.Rejected.Count == 1)
            {
                Console.WriteLine("  ✓ Same-batch duplicate handling working correctly");
            }
            else
            {
                Console.WriteLine("  ✗ Same-batch duplicate handling NOT working correctly");
            }

            // Now test cross-batch (different batch) duplicate handling
            Console.WriteLine("  Testing cross-batch duplicate handling...");
            
            // First batch with the event
            var firstBatch = new List<Event>
            {
                new Event
                {
                    EventId = "cross_batch_duplicate",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "click",
                    Timestamp = 1234567890000,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button1" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            var firstResult = await _validationService.ValidateBatchEventsAsync(firstBatch);
            
            // Save the accepted events
            if (firstResult.Accepted.Any())
            {
                await _eventStore.AppendEventsAsync(firstBatch.Where(e => firstResult.Accepted.Any(a => a.EventId == e.EventId)).ToList());
            }

            // Second batch with the same event ID (cross-batch duplicate)
            var secondBatch = new List<Event>
            {
                new Event
                {
                    EventId = "cross_batch_duplicate", // Same ID as first batch
                    ProductId = "product1",
                    CompanyId = "company1", 
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1", 
                    EventType = "click",
                    Timestamp = 1234567890000,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button2" }, // Different content
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            var secondResult = await _validationService.ValidateBatchEventsAsync(secondBatch);
            Console.WriteLine($"  Cross-batch validation - Accepted: {secondResult.Accepted.Count}, Rejected: {secondResult.Rejected.Count}");
            Console.WriteLine($"  Expected: 1 accepted (cross-batch duplicates are accepted per spec)");

            if (secondResult.Accepted.Count == 1 && secondResult.Rejected.Count == 0)
            {
                Console.WriteLine("  ✓ Cross-batch duplicate handling working correctly");
            }
            else
            {
                Console.WriteLine("  ✗ Cross-batch duplicate handling NOT working correctly");
            }
        }

        public async Task VerifyCursorIteration()
        {
            Console.WriteLine("\nTesting cursor iteration functionality...");

            // Add multiple events to test cursor functionality
            var events = new List<Event>
            {
                new Event
                {
                    EventId = "cursor_event_1",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen1",
                    EventType = "click",
                    Timestamp = 1234567890000,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { ComponentId = "button1" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                },
                new Event
                {
                    EventId = "cursor_event_2",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen2",
                    EventType = "enter_screen",
                    Timestamp = 1234567890001,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { View = "home" },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                },
                new Event
                {
                    EventId = "cursor_event_3",
                    ProductId = "product1",
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    SessionId = "session1",
                    ScreenId = "screen3",
                    EventType = "leave_screen",
                    Timestamp = 1234567890002,
                    DeviceId = "device1",
                    Metadata = new EventMetadata { Duration = 10.5 },
                    DeviceInfo = new DeviceInfo { DeviceType = "Browser", Os = "Windows" }
                }
            };

            await _eventStore.AppendEventsAsync(events);

            // Test initial query (no cursor)
            var firstPage = await _eventStore.GetEventsAsync(null, 2); // Size = 2
            Console.WriteLine($"  First page: {firstPage.Count} events");

            // Build next cursor from the last event in first page
            var nextCursor = _cursorHandler.BuildNextCursor(firstPage);
            Console.WriteLine($"  Next cursor: {nextCursor}");

            // Query with cursor to get next page
            var secondPage = await _eventStore.GetEventsAsync(nextCursor, 2);
            Console.WriteLine($"  Second page with cursor: {secondPage.Count} events");

            // Check if cursor correctly skips the first page
            if (firstPage.Count > 0 && secondPage.Count > 0)
            {
                var firstPageLastEvent = firstPage.Last();
                var secondPageFirstEvent = secondPage.First();

                // Compare based on the ordering (timestamp-CompanyId-EmployeeId-DeviceType-DeviceId)
                var comparison = CompareEventsForOrdering(firstPageLastEvent, secondPageFirstEvent);
                
                if (comparison < 0) // second event should come after first
                {
                    Console.WriteLine("  ✓ Cursor iteration working correctly");
                }
                else
                {
                    Console.WriteLine("  ✗ Cursor iteration NOT working correctly");
                }
            }
            else
            {
                Console.WriteLine("  ✗ Not enough events to test cursor iteration properly");
            }
        }

        private int CompareEventsForOrdering(Event evt1, Event evt2)
        {
            // Compare by timestamp first
            var timestampComparison = evt1.Timestamp.CompareTo(evt2.Timestamp);
            if (timestampComparison != 0)
                return timestampComparison;

            // Then by companyId
            var companyComparison = string.Compare(evt1.CompanyId, evt2.CompanyId, StringComparison.Ordinal);
            if (companyComparison != 0)
                return companyComparison;

            // Then by employeeId
            var employeeComparison = string.Compare(evt1.EmployeeId, evt2.EmployeeId, StringComparison.Ordinal);
            if (employeeComparison != 0)
                return employeeComparison;

            // Then by deviceType
            var deviceTypeComparison = string.Compare(evt1.DeviceInfo.DeviceType, evt2.DeviceInfo.DeviceType, StringComparison.Ordinal);
            if (deviceTypeComparison != 0)
                return deviceTypeComparison;

            // Finally by deviceId
            return string.Compare(evt1.DeviceId, evt2.DeviceId, StringComparison.Ordinal);
        }

        public async Task RunAllVerifications()
        {
            Console.WriteLine("Starting manual verification tests...\n");
            
            await VerifyPostEventsBatchPartialSuccess();
            await VerifyQueryApiOrdering();
            await VerifyEventIdDuplicateHandling();
            await VerifyCursorIteration();
            
            Console.WriteLine("\nManual verification tests completed.");
        }
    }
}