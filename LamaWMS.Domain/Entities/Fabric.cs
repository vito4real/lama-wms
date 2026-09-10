namespace LamaWMS.Domain.Entities;

public class Fabric
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Color { get; set; } = null!;

    public ICollection<Roll> Rolls { get; set; } = new List<Roll>();

    public ICollection<ProductFabric> ProductFabrics { get; set; } = new List<ProductFabric>();
}