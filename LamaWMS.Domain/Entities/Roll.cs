namespace LamaWMS.Domain.Entities;

public class Roll
{
    public Guid Id { get; set; }

    public string Number { get; set; } = null!;

    public decimal InitialLength { get; set; }

    public decimal CurrentLength { get; set; }

    public Guid FabricId { get; set; }

    public Fabric Fabric { get; set; } = null!;

    public Guid? CellId { get; set; }

    public Cell? Cell { get; set; }

    public ICollection<RollTransaction> Transactions { get; set; }
        = new List<RollTransaction>();
}