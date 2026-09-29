using LamaWMS.Domain.Entities;

public class RollReservation
{
    public Guid Id { get; set; }

    public Guid RollId { get; set; }

    public Roll Roll { get; set; } = null!;

    public Guid ProductFabricId { get; set; }

    public ProductFabric ProductFabric { get; set; } = null!;

    public decimal ReservedLength { get; set; }

    public DateTime CreatedAt { get; set; }

    public string RequestNumber { get; set; } = null!;
}