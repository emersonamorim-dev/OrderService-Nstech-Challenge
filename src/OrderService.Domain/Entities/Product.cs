using OrderService.Domain.Exceptions;

namespace OrderService.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int AvailableQuantity { get; private set; }
    public int Version { get; private set; }

    private Product()
    {
    }

    public Product(Guid id, string name, decimal unitPrice, int availableQuantity)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        if (availableQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availableQuantity), "Available quantity cannot be negative.");
        }

        Id = id;
        Name = name.Trim();
        UnitPrice = unitPrice;
        AvailableQuantity = availableQuantity;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity to reserve must be greater than zero.");
        }

        if (AvailableQuantity < quantity)
        {
            throw new InsufficientStockException(Id, quantity, AvailableQuantity);
        }

        AvailableQuantity -= quantity;
        Version++;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity to release must be greater than zero.");
        }

        AvailableQuantity += quantity;
        Version++;
    }

    public bool HasAvailability(int quantity) => AvailableQuantity >= quantity;
}
