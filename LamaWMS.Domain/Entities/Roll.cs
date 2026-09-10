using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LamaWMS.Domain.Entities;

public class Roll
{
    public Guid Id { get; set; }

    public string Number { get; set; } = null!;

    public decimal InitialLength { get; set; }

    public decimal CurrentLength { get; set; }

    public Guid FabricId { get; set; }

    [ValidateNever]
    public Fabric Fabric { get; set; } = null!;

    public Guid? CellId { get; set; }

    [ValidateNever]
    public Cell? Cell { get; set; }

    [ValidateNever]
    public ICollection<RollTransaction> Transactions { get; set; }
        = new List<RollTransaction>();

    [ValidateNever]
    public ICollection<RollReservation> Reservations { get; set; }
    = new List<RollReservation>();
}