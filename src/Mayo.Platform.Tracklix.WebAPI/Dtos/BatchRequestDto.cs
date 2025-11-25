using Mayo.Platform.Tracklix.WebAPI.Models;

namespace Mayo.Platform.Tracklix.WebAPI.Dtos
{
    public class BatchRequestDto
    {
        public List<Event> Events { get; set; } = new List<Event>();
    }
}