using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Domain.Services.Orders;

public sealed class OrderItemChangeAuditManager(
    IRepository<OrderItemChangeAudit> auditRepository,
    IEntityDataReader<OrderItemChangeAudit> auditReader) : IOrderItemChangeAuditManager
{
    public async Task RecordAsync(CreateOrderItemChangeAuditDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        await auditRepository.InsertAsync(OrderItemChangeAudit.Create(dto)).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<OrderItemChangeAuditDto>> GetByOrderIdAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
            return Task.FromResult<IReadOnlyList<OrderItemChangeAuditDto>>([]);

        var audits = auditReader.DataSource
            .Where(audit => audit.OrderId == orderId)
            .OrderBy(audit => audit.CreatedOnUtc)
            .ThenBy(audit => audit.Id)
            .ToList()
            .Select(audit => audit.ToDto())
            .ToList();

        return Task.FromResult<IReadOnlyList<OrderItemChangeAuditDto>>(audits);
    }
}
