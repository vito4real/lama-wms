namespace LamaWMS.Domain.Entities;

public class FurnitureLabelPrintEvent
{
    public Guid Id { get; set; }
    public Guid FurnitureLabelId { get; set; }
    public FurnitureLabel FurnitureLabel { get; set; } = null!;
    public int Copies { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? RequestedByUserId { get; set; }
}
