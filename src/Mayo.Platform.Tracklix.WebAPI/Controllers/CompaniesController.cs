using Mayo.Platform.Tracklix.WebAPI.Dtos;
using Mayo.Platform.Tracklix.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mayo.Platform.Tracklix.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly IEventStore _eventStore;
        private readonly EventCursorHandler _cursorHandler;

        public CompaniesController(IEventStore eventStore, EventCursorHandler cursorHandler)
        {
            _eventStore = eventStore;
            _cursorHandler = cursorHandler;
        }

        [HttpGet("{companyId}/events")]
        public async Task<ActionResult<QueryResponseDto>> GetCompanyEvents(string companyId, [FromQuery] string? t, [FromQuery] int size = 100)
        {
            // Validate companyId parameter
            if (string.IsNullOrEmpty(companyId))
            {
                return BadRequest("companyId is required");
            }

            // Limit size to reasonable bounds if needed
            size = Math.Min(size, 1000); // Set a reasonable upper limit
            
            var events = await _eventStore.GetEventsByCompanyAsync(companyId, t, size);
            var nextCursor = _cursorHandler.BuildNextCursor(events);

            var response = new QueryResponseDto
            {
                Events = events,
                NextCursor = nextCursor,
                Size = events.Count
            };

            return Ok(response);
        }
    }
}