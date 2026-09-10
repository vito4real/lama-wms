using LamaWMS.Domain.Entities;

namespace LamaWMS.Domain.Entities;

public class ProductFabric
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public Guid FabricId { get; set; }

    public Fabric Fabric { get; set; } = null!;

    public decimal RequiredLength { get; set; }

    public ICollection<RollReservation> RollReservations { get; set; }
        = new List<RollReservation>();
}