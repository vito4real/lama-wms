using LamaWMS.Domain.Enums;

namespace LamaWMS.Domain.Entities;

public class RollTransaction
{
    public Guid Id { get; set; }

    public Guid RollId { get; set; }

    public Roll Roll { get; set; } = null!;

    public RollTransactionType Type { get; set; }

    public decimal Length { get; set; }

    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Comment { get; set; }
}