namespace OrderService.Domain.ValueObjects;

public readonly record struct Quantity
{
    public int Value { get; }

    public Quantity(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Quantity must be greater than zero.");
        }

        Value = value;
    }

    public static implicit operator int(Quantity quantity) => quantity.Value;
}
