using LamaWMS.Domain.Entities;

namespace LamaWMS.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    public string Number { get; set; } = null!;

    public string CustomerName { get; set; } = null!;
}

