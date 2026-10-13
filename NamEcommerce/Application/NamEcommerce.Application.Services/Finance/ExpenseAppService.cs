using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Returns;

namespace NamEcommerce.Application.Services.Finance;

public class ExpenseAppService(IExpenseManager expenseManager, IEntityDataReader<Expense> expenseDataReader,
    IEntityDataReader<Order> orderDataReader, IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
    IEntityDataReader<CustomerReturn> customerReturnDataReader, IEntityDataReader<VendorReturn> vendorReturnDataReader) : IExpenseAppService
{
    public async Task<IPagedDataAppDto<ExpenseAppDto>> GetExpensesAsync(
        int pageIndex = 0, int pageSize = int.MaxValue, IList<Guid>? orderIds = null,
        string? keywords = null, DateTime? fromDate = null, DateTime? toDate = null,
        int? expenseType = null, string? sortBy = null, bool sortDesc = true)
    {
        var expenseSortBy = sortBy switch
        {
            "amount" => sortDesc ? ExpenseSortByEnum.AmountDesc : ExpenseSortByEnum.Amount,
            "title" => sortDesc ? ExpenseSortByEnum.TitleDesc : ExpenseSortByEnum.Title,
            "type" => ExpenseSortByEnum.ExpenseType,
            _ => sortDesc ? ExpenseSortByEnum.IncurredDateDesc : ExpenseSortByEnum.IncurredDate
        };
        var pagedData = await expenseManager.GetExpensesAsync(pageIndex, pageSize,
            orderIds, keywords, fromDate, toDate,
            (ExpenseType?)expenseType, expenseSortBy).ConfigureAwait(false);

        return pagedData.ToPagedDataAppDto(expense => new ExpenseAppDto
        {
            Id = expense.Id,
            Title = expense.Title,
            Description = expense.Description,
            AmountWithoutTax = expense.AmountWithoutTax,
            TaxRate = expense.TaxRate,
            Amount = expense.Amount,
            ExpenseType = (int)expense.ExpenseType,
            IncurredDateUtc = expense.IncurredDate,
            ReferenceId = expense.ReferenceId,
            ReferenceCode = expense.ReferenceCode,
            ReferenceType = (int) expense.ReferenceType,
            IsSystemGenerated = expense.IsSystemGenerated
        });
    }

    public async Task<ExpenseAppDto?> GetExpenseByIdAsync(Guid id)
    {
        var expense = await expenseManager.GetExpenseByIdAsync(id).ConfigureAwait(false);

        if (expense is null)
            return null;

        return new()
        {
            Id = expense.Id,
            Title = expense.Title,
            Description = expense.Description,
            AmountWithoutTax = expense.AmountWithoutTax,
            TaxRate = expense.TaxRate,
            Amount = expense.Amount,
            ExpenseType = (int)expense.ExpenseType,
            IncurredDateUtc = expense.IncurredDate,
            ReferenceId = expense.ReferenceId,
            ReferenceCode = expense.ReferenceCode,
            ReferenceType = (int) expense.ReferenceType,
            IsSystemGenerated = expense.IsSystemGenerated
        };
    }

    public async Task<CreateExpenseResultAppDto> CreateGeneralExpenseAsync(CreateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new CreateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var result = await expenseManager.CreateExpenseAsync(new CreateExpenseDto
        {
            Title = dto.Title,
            Description = dto.Description,
            AmountWithoutTax = dto.AmountWithoutTax,
            TaxRate = dto.TaxRate,
            ExpenseType = (ExpenseType)dto.ExpenseType,
            IncurredDateUtc = dto.IncurredDateUtc,
            RecordedByUserId = dto.RecordedByUserId
        }).ConfigureAwait(false);

        return new CreateExpenseResultAppDto { Success = true, CreatedId = result.CreatedId };
    }
    public async Task<CreateExpenseResultAppDto> CreateOrderExpenseAsync(Guid orderId, CreateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new CreateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var order = await orderDataReader.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            return new CreateExpenseResultAppDto { Success = false, ErrorMessage = "Error.OrderIsNotFound" };

        var result = await expenseManager.CreateExpenseAsync(new CreateExpenseDto
        {
            Title = dto.Title,
            Description = dto.Description,
            AmountWithoutTax = dto.AmountWithoutTax,
            TaxRate = dto.TaxRate,
            ExpenseType = (ExpenseType)dto.ExpenseType,
            IncurredDateUtc = dto.IncurredDateUtc,
            RecordedByUserId = dto.RecordedByUserId,
            ReferenceId = orderId,
            ReferenceCode = order.Code,
            ReferenceType = ExpenseReferenceType.Order
        }).ConfigureAwait(false);

        return new CreateExpenseResultAppDto { Success = true, CreatedId = result.CreatedId };
    }
    public async Task<CreateExpenseResultAppDto> CreatePurchaseOrderExpenseAsync(Guid purchaseOrderId, CreateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new CreateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var purchaseOrder = await purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return new CreateExpenseResultAppDto { Success = false, ErrorMessage = "Error.PurchaseOrderIsNotFound" };

        var result = await expenseManager.CreateExpenseAsync(new CreateExpenseDto
        {
            Title = dto.Title,
            Description = dto.Description,
            AmountWithoutTax = dto.AmountWithoutTax,
            TaxRate = dto.TaxRate,
            ExpenseType = (ExpenseType)dto.ExpenseType,
            IncurredDateUtc = dto.IncurredDateUtc,
            RecordedByUserId = dto.RecordedByUserId,
            ReferenceId = purchaseOrderId,
            ReferenceCode = purchaseOrder.Code,
            ReferenceType = ExpenseReferenceType.PurchaseOrder
        }).ConfigureAwait(false);

        return new CreateExpenseResultAppDto { Success = true, CreatedId = result.CreatedId };
    }
    public async Task<CreateExpenseResultAppDto> CreateCustomerReturnExpenseAsync(Guid customerReturnId, CreateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new CreateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var customerReturn = await customerReturnDataReader.GetByIdAsync(customerReturnId).ConfigureAwait(false);
        if (customerReturn is null)
            return new CreateExpenseResultAppDto { Success = false, ErrorMessage = "Error.CustomerReturnIsNotFound" };

        var result = await expenseManager.CreateExpenseAsync(new CreateExpenseDto
        {
            Title = dto.Title,
            Description = dto.Description,
            AmountWithoutTax = dto.AmountWithoutTax,
            TaxRate = dto.TaxRate,
            ExpenseType = (ExpenseType)dto.ExpenseType,
            IncurredDateUtc = dto.IncurredDateUtc,
            RecordedByUserId = dto.RecordedByUserId,
            ReferenceId = customerReturnId,
            ReferenceCode = customerReturn.Code,
            ReferenceType = ExpenseReferenceType.CustomerReturn
        }).ConfigureAwait(false);

        return new CreateExpenseResultAppDto { Success = true, CreatedId = result.CreatedId };
    }
    public async Task<CreateExpenseResultAppDto> CreateVendorReturnExpenseAsync(Guid vendorReturnId, CreateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new CreateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var vendorReturn = await vendorReturnDataReader.GetByIdAsync(vendorReturnId).ConfigureAwait(false);
        if (vendorReturn is null)
            return new CreateExpenseResultAppDto { Success = false, ErrorMessage = "Error.VendorReturnIsNotFound" };

        var result = await expenseManager.CreateExpenseAsync(new CreateExpenseDto
        {
            Title = dto.Title,
            Description = dto.Description,
            AmountWithoutTax = dto.AmountWithoutTax,
            TaxRate = dto.TaxRate,
            ExpenseType = (ExpenseType)dto.ExpenseType,
            IncurredDateUtc = dto.IncurredDateUtc,
            RecordedByUserId = dto.RecordedByUserId,
            ReferenceId = vendorReturnId,
            ReferenceCode = vendorReturn.Code,
            ReferenceType = ExpenseReferenceType.VendorReturn
        }).ConfigureAwait(false);

        return new CreateExpenseResultAppDto { Success = true, CreatedId = result.CreatedId };
    }


    public async Task<UpdateExpenseResultAppDto> UpdateExpenseAsync(UpdateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new UpdateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var expense = await expenseDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (expense is null)
            return new UpdateExpenseResultAppDto { Success = false, ErrorMessage = "Error.ExpenseIsNotFound" };

        await expenseManager.UpdateExpenseAsync(new UpdateExpenseDto
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            AmountWithoutTax = dto.AmountWithoutTax,
            TaxRate = dto.TaxRate,
            IncurredDateUtc = dto.IncurredDateUtc,
            ExpenseType = (ExpenseType)dto.ExpenseType
        }).ConfigureAwait(false);

        return new UpdateExpenseResultAppDto { Success = true, UpdatedId = dto.Id };
    }

    public async Task<DeleteExpenseResultAppDto> DeleteExpenseAsync(Guid id)
    {
        var expense = await expenseDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (expense is null) return new DeleteExpenseResultAppDto { Success = false, ErrorMessage = "Error.ExpenseIsNotFound" };

        await expenseManager.DeleteExpenseAsync(id).ConfigureAwait(false);
        return new DeleteExpenseResultAppDto { Success = true };
    }

    public async Task<IEnumerable<ExpenseSummaryAppDto>> GetExpenseSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var data = await expenseManager.GetExpensesAsync(0, int.MaxValue, fromDate: fromDate, toDate: toDate).ConfigureAwait(false);
        var summaryLines = data.GroupBy(expense => expense.ExpenseType)
            .Select(group => new ExpenseSummaryAppDto
            {
                ExpenseType = (int)group.Key,
                Count = group.Count(),
                TotalAmount = group.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        return summaryLines;
    }
}
