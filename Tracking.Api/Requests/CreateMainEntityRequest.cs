using System.ComponentModel.DataAnnotations;
using Tracking.Api.Models;

namespace Tracking.Api.Requests;

public sealed class CreateMainEntityRequest
{
    [Required]
    public ulong CreatorId { get; set; }

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    [EmailAddress]
    public string CreatorEmail { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;
    public string Production {get;set;}="";

    public string Panels { get; set; } = "{}";

    public string Collaborators { get; set; } = "[]";

    public string Visibility { get; set; } = "private";

    public bool IsShared { get; set; }

    public Guid? SharedToken { get; set; }

    public MainEntity ToMainEntity(Guid entityId)
    {
        var now = DateTime.UtcNow;
        return new MainEntity
        {
            EntityId = entityId,
            CreatorId = CreatorId,
            CompanyId = CompanyId,
            CreatorEmail = CreatorEmail,
            Title = Title,
            Production = Production,
            Panels = Panels,
            Collaborators = Collaborators,
            Visibility = Visibility,
            IsShared = IsShared,
            SharedToken = SharedToken ?? Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
