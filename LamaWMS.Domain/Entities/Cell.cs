using LamaWMS.Domain.Entities;

namespace LamaWMS.Domain.Entities;

public class Cell
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public ICollection<Roll> Rolls { get; set; } = new List<Roll>();
}