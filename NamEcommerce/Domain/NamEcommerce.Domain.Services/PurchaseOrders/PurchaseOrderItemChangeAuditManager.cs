using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Domain.Services.PurchaseOrders;

public sealed class PurchaseOrderItemChangeAuditManager(
    IRepository<PurchaseOrderItemChangeAudit> auditRepository,
    IEntityDataReader<PurchaseOrderItemChangeAudit> auditReader) : IPurchaseOrderItemChangeAuditManager
{
    public async Task RecordAsync(CreatePurchaseOrderItemChangeAuditDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        await auditRepository.InsertAsync(PurchaseOrderItemChangeAudit.Create(dto)).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<PurchaseOrderItemChangeAuditDto>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId)
    {
        if (purchaseOrderId == Guid.Empty)
            return Task.FromResult<IReadOnlyList<PurchaseOrderItemChangeAuditDto>>([]);

        var audits = auditReader.DataSource
            .Where(audit => audit.PurchaseOrderId == purchaseOrderId)
            .OrderBy(audit => audit.CreatedOnUtc)
            .ThenBy(audit => audit.Id)
            .ToList()
            .Select(audit => audit.ToDto())
            .ToList();

        return Task.FromResult<IReadOnlyList<PurchaseOrderItemChangeAuditDto>>(audits);
    }
}
