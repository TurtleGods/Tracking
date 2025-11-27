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


    public string Panels { get; set; } = "{}";

    public string Collaborators { get; set; } = "[]";

    public string Visibility { get; set; } = "private";

    public bool IsShared { get; set; }

    public Guid? SharedToken { get; set; }

    public MainEntity ToMainEntity(MainEntity mainEntity)
    {
        return new MainEntity
        {
            EntityId = mainEntity.EntityId,
            CreatorId = mainEntity.CreatorId,
            CompanyId = mainEntity.CompanyId,
            CreatorEmail = mainEntity.CreatorEmail,
            Production = mainEntity.Production,
            Panels = mainEntity.Panels,
            Collaborators = mainEntity.Collaborators,
            Visibility = mainEntity.Visibility,
            IsShared = mainEntity.IsShared,
            SharedToken = mainEntity.SharedToken,
            CreatedAt = mainEntity.CreatedAt,
            UpdatedAt = mainEntity.UpdatedAt
        };
    }
}
