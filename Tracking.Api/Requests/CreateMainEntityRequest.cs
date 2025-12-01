using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using Tracking.Api.Models;

namespace Tracking.Api.Requests;

[ExcludeFromCodeCoverage]
public sealed class CreateMainEntityRequest
{

    [Required]
    public Guid CompanyId { get; set; }


    public MainEntity ToMainEntity(MainEntity mainEntity)
    {
        return new MainEntity
        {
            EntityId = mainEntity.EntityId,
            CompanyId = mainEntity.CompanyId,
            Production = mainEntity.Production,
            CreatedAt = mainEntity.CreatedAt,
            UpdatedAt = mainEntity.UpdatedAt
        };
    }
}
