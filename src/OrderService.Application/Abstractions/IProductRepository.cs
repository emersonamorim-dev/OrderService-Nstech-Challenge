using OrderService.Domain.Entities;

namespace OrderService.Application.Abstractions;

public interface IProductRepository
{
    /// <summary>Read-only product lookup (no tracking).</summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Tracked products for stock mutation inside a transaction.
    /// Concurrency is enforced via <c>Product.Version</c> optimistic token on SaveChanges.
    /// </summary>
    Task<IReadOnlyList<Product>> GetTrackedByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
}
