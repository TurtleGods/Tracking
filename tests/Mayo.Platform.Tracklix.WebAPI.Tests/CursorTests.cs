using Mayo.Platform.Tracklix.WebAPI.Services;

namespace Mayo.Platform.Tracklix.WebAPI.Tests
{
    public class CursorTests
    {
        private readonly EventCursorHandler _cursorHandler;

        public CursorTests()
        {
            _cursorHandler = new EventCursorHandler();
        }

        [Fact]
        public void BuildNextCursor_CreatesValidCursor()
        {
            // Arrange
            var events = new List<Models.Event>
            {
                new Models.Event
                {
                    Timestamp = 1234567890,
                    CompanyId = "company1",
                    EmployeeId = "employee1",
                    DeviceInfo = new Models.DeviceInfo { DeviceType = "Browser" },
                    DeviceId = "device1"
                }
            };

            // Act
            var cursor = _cursorHandler.BuildNextCursor(events);

            // Assert
            Assert.Equal("1234567890|company1|employee1|Browser|device1", cursor);
        }

        [Fact]
        public void BuildNextCursor_ReturnsNullForEmptyList()
        {
            // Arrange
            var events = new List<Models.Event>();

            // Act
            var cursor = _cursorHandler.BuildNextCursor(events);

            // Assert
            Assert.Null(cursor);
        }

        [Fact]
        public void ParseCursor_ParsesValidCursor()
        {
            // Arrange
            var cursor = "1234567890|company1|employee1|Browser|device1";

            // Act
            var result = _cursorHandler.ParseCursor(cursor);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1234567890L, result.Value.timestamp);
            Assert.Equal("company1", result.Value.companyId);
            Assert.Equal("employee1", result.Value.employeeId);
            Assert.Equal("Browser", result.Value.deviceType);
            Assert.Equal("device1", result.Value.deviceId);
        }

        [Fact]
        public void ParseCursor_ReturnsNullForInvalidFormat()
        {
            // Arrange
            var invalidCursors = new[] { 
                "invalid", 
                "1234567890|company1|employee1",  // Missing parts
                "not_a_number|company1|employee1|Browser|device1",  // Invalid timestamp
                "|company1|employee1|Browser|device1"  // Empty timestamp
            };

            // Act & Assert
            foreach (var invalidCursor in invalidCursors)
            {
                var result = _cursorHandler.ParseCursor(invalidCursor);
                Assert.Null(result);
            }
        }

        [Fact]
        public void ParseCursor_ReturnsNullForNullOrEmpty()
        {
            // Act
            var result1 = _cursorHandler.ParseCursor(null);
            var result2 = _cursorHandler.ParseCursor("");

            // Assert
            Assert.Null(result1);
            Assert.Null(result2);
        }
    }
}