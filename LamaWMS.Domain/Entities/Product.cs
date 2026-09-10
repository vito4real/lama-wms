namespace LamaWMS.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public ICollection<ProductFabric> ProductFabrics { get; set; }
        = new List<ProductFabric>();

    public ICollection<RollTransaction> RollTransactions { get; set; }
        = new List<RollTransaction>();
}