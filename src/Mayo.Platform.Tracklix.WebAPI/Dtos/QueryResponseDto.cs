using Mayo.Platform.Tracklix.WebAPI.Models;

namespace Mayo.Platform.Tracklix.WebAPI.Dtos
{
    public class QueryResponseDto
    {
        public List<Event> Events { get; set; } = new List<Event>();
        public string? NextCursor { get; set; }
        public int Size { get; set; }
    }
}