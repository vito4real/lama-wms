using LamaWMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LamaWMS.Domain.Entities;

public class ProductFabric
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    [ValidateNever]
    public Product Product { get; set; } = null!;

    public Guid FabricId { get; set; }
    
    [ValidateNever]
    public Fabric Fabric { get; set; } = null!;

    public decimal RequiredLength { get; set; }
    
    [ValidateNever]
    public ICollection<RollReservation> RollReservations { get; set; }
        = new List<RollReservation>();
}