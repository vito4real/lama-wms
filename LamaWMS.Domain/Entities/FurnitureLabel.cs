namespace LamaWMS.Domain.Entities;

public class FurnitureLabel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Complectation { get; set; } = "";
    public string Designation { get; set; } = "";
    public string FunctionalPurpose { get; set; } = "";
    public string DeclarationNumber { get; set; } = "";
    public DateOnly DeclarationValidFrom { get; set; }
    public DateOnly DeclarationValidTo { get; set; }
    public string RequestNumber { get; set; } = "";
    public string FabricGroup { get; set; } = "";
    public DateOnly ManufactureDate { get; set; }
    public int Copies { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public ICollection<FurnitureLabelPrintEvent> PrintEvents { get; set; } = new List<FurnitureLabelPrintEvent>();
}
