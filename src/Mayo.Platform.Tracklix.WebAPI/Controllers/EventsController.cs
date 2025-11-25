using Mayo.Platform.Tracklix.WebAPI.Dtos;
using Mayo.Platform.Tracklix.WebAPI.Models;
using Mayo.Platform.Tracklix.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mayo.Platform.Tracklix.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventStore _eventStore;
        private readonly ValidationService _validationService;
        private readonly EventCursorHandler _cursorHandler;

        public EventsController(IEventStore eventStore, ValidationService validationService, EventCursorHandler cursorHandler)
        {
            _eventStore = eventStore;
            _validationService = validationService;
            _cursorHandler = cursorHandler;
        }

        [HttpPost("batch")]
        public async Task<ActionResult<BatchEventResponse>> ReceiveBatchEvents(BatchRequestDto request)
        {
            if (request?.Events == null || !request.Events.Any())
            {
                return BadRequest(new BatchEventResponse());
            }

            // Validate the batch
            var validationResult = await _validationService.ValidateBatchEventsAsync(request.Events);
            
            // If there are any accepted events, save them
            if (validationResult.Accepted.Any())
            {
                var acceptedEvents = new List<Event>();
                
                foreach (var accepted in validationResult.Accepted)
                {
                    var eventToSave = request.Events.FirstOrDefault(e => e.EventId == accepted.EventId);
                    if (eventToSave != null)
                    {
                        acceptedEvents.Add(eventToSave);
                    }
                }
                
                if (acceptedEvents.Any())
                {
                    await _eventStore.AppendEventsAsync(acceptedEvents);
                }
            }

            return Ok(validationResult);
        }

        [HttpGet]
        public async Task<ActionResult<QueryResponseDto>> GetEvents([FromQuery] string? t, [FromQuery] int size = 100)
        {
            // Limit size to reasonable bounds if needed
            size = Math.Min(size, 1000); // Set a reasonable upper limit
            
            var events = await _eventStore.GetEventsAsync(t, size);
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