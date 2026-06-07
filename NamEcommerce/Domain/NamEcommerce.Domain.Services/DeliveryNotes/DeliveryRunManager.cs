using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.DeliveryNotes;

public sealed class DeliveryRunManager(
    IDbContext dbContext,
    IRepository<DeliveryRun> runRepository,
    IEntityDataReader<DeliveryRun> runReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IDeliveryNoteManager deliveryNoteManager,
    ICustomerDebtManager customerDebtManager,
    ICurrentUserAccessor currentUserAccessor) : IDeliveryRunManager
{
    private string GenerateCode()
    {
        var prefix = $"DVR-{DateTime.UtcNow:yyyyMMdd}";
        var count = runReader.SecuredDataSource.Count(run => run.Code.StartsWith(prefix));
        return $"{prefix}-{(count + 1):D3}";
    }

    public async Task<DeliveryRunDto> CreateAsync(CreateDeliveryRunDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var noteIds = dto.DeliveryNoteIds.Distinct().ToList();
        var activeNoteIds = runReader.DataSource
            .Where(run => run.Status != DeliveryRunStatus.Closed && run.Status != DeliveryRunStatus.Cancelled)
            .SelectMany(run => run.Items)
            .Where(item => noteIds.Contains(item.DeliveryNoteId))
            .Select(item => item.DeliveryNoteId)
            .ToList();
        if (activeNoteIds.Count > 0)
            throw new NamEcommerceDomainException("Error.DeliveryRunDeliveryNoteAlreadyActive");

        var notes = deliveryNoteReader.DataSource
            .Where(note => noteIds.Contains(note.Id))
            .ToList();
        if (notes.Count != noteIds.Count)
            throw new NamEcommerceDomainException("Error.DeliveryNoteNotFound");

        foreach (var note in notes)
        {
            if (note.Status != DeliveryNoteStatus.Confirmed)
                throw new NamEcommerceDomainException("Error.DeliveryRunDeliveryNoteMustBeConfirmed");
            if (note.IsDirectShip || note.SourceType != DeliveryNoteSourceType.ToCustomer)
                throw new NamEcommerceDomainException("Error.DeliveryRunDeliveryNoteSourceNotSupported");
            if (note.AssignedDeliveryUserId != dto.AssignedDeliveryUserId)
                throw new NamEcommerceDomainException("Error.DeliveryRunDeliveryNoteDriverMismatch");
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var run = new DeliveryRun(
            GenerateCode(),
            dto.AssignedDeliveryUserId,
            dto.AssignedDeliveryUsername,
            dto.AssignedDeliveryFullName,
            dto.Note,
            currentUser?.Id);

        foreach (var noteId in noteIds)
        {
            var note = notes.First(n => n.Id == noteId);
            run.AddItem(
                note.Id,
                note.Code,
                note.OrderCode,
                note.CustomerInfo.FullName.Value,
                note.ShippingAddress.Value,
                note.AmountToCollect);
        }

        var inserted = await runRepository.InsertAsync(run).ConfigureAwait(false);
        return inserted.ToDto();
    }

    public async Task AcknowledgeDriverCacheAsync(Guid id, string? deviceId)
    {
        var run = await runRepository.GetByIdAsync(id).ConfigureAwait(false)
                  ?? throw new NamEcommerceDomainException("Error.DeliveryRunNotFound");

        run.AcknowledgeDriverCache(deviceId, DateTime.UtcNow);
        await runRepository.UpdateAsync(run).ConfigureAwait(false);
    }

    public async Task IssuePaperManifestAsync(Guid id)
    {
        var run = await runRepository.GetByIdAsync(id).ConfigureAwait(false)
                  ?? throw new NamEcommerceDomainException("Error.DeliveryRunNotFound");

        run.IssuePaperManifest(DateTime.UtcNow);
        await runRepository.UpdateAsync(run).ConfigureAwait(false);
    }

    public async Task HandOverAsync(Guid id)
    {
        await using var transaction = await dbContext.BeginTransactionAsync().ConfigureAwait(false);
        var run = await runRepository.GetByIdAsync(id).ConfigureAwait(false)
                  ?? throw new NamEcommerceDomainException("Error.DeliveryRunNotFound");
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);

        run.HandOver(currentUser?.Id, DateTime.UtcNow);
        foreach (var item in run.Items)
            await deliveryNoteManager.MarkDeliveringAsync(item.DeliveryNoteId).ConfigureAwait(false);

        await runRepository.UpdateAsync(run).ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
    }

    public async Task CloseAsync(Guid id)
    {
        var run = await runRepository.GetByIdAsync(id).ConfigureAwait(false)
                  ?? throw new NamEcommerceDomainException("Error.DeliveryRunNotFound");

        var noteIds = run.Items.Select(item => item.DeliveryNoteId).ToList();
        var notes = deliveryNoteReader.DataSource
            .Where(note => noteIds.Contains(note.Id))
            .ToList();

        if (notes.Any(note => note.Status != DeliveryNoteStatus.Delivered && note.Status != DeliveryNoteStatus.Cancelled))
            throw new NamEcommerceDomainException("Error.DeliveryRunCannotCloseWithOpenNotes");

        run.Close(DateTime.UtcNow);
        await runRepository.UpdateAsync(run).ConfigureAwait(false);
    }

    public async Task ConfirmCashHandoverAsync(ConfirmDeliveryRunCashHandoverDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var run = await runRepository.GetByIdAsync(dto.DeliveryRunId).ConfigureAwait(false)
                  ?? throw new NamEcommerceDomainException("Error.DeliveryRunNotFound");
        if (run.Status is DeliveryRunStatus.ReadyForHandover or DeliveryRunStatus.Cancelled)
            throw new NamEcommerceDomainException("Error.DeliveryRunCannotConfirmCashHandover");

        var noteIds = run.Items.Select(item => item.DeliveryNoteId).ToList();
        var deliveredNotes = deliveryNoteReader.DataSource
            .Where(note => noteIds.Contains(note.Id) && note.Status == DeliveryNoteStatus.Delivered)
            .ToList();
        var expectedAmount = deliveredNotes.Sum(GetCashCollectedAmount);
        if (expectedAmount <= 0)
            throw new NamEcommerceDomainException("Error.DeliveryRunCashHandoverNotRequired");
        if (Math.Abs(dto.Amount - expectedAmount) > 0.0001m)
            throw new NamEcommerceDomainException("Error.CashHandoverAmountMustMatchCollectedAmount");

        await using var transaction = await dbContext.BeginTransactionAsync().ConfigureAwait(false);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var confirmedOnUtc = DateTime.UtcNow;
        run.ConfirmCashHandover(
            currentUser?.Id,
            currentUser?.Username,
            currentUser?.FullName,
            dto.Amount,
            dto.Note,
            confirmedOnUtc);
        await RecordCodPaymentsAsync(run, deliveredNotes, currentUser?.Id, confirmedOnUtc, dto.Note)
            .ConfigureAwait(false);
        await runRepository.UpdateAsync(run).ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
    }

    public async Task CancelAsync(Guid id)
    {
        var run = await runRepository.GetByIdAsync(id).ConfigureAwait(false)
                  ?? throw new NamEcommerceDomainException("Error.DeliveryRunNotFound");

        run.Cancel();
        await runRepository.UpdateAsync(run).ConfigureAwait(false);
    }

    public async Task<DeliveryRunDto?> GetByIdAsync(Guid id)
    {
        var run = await runReader.GetByIdAsync(id).ConfigureAwait(false);
        return run?.ToDto();
    }

    public Task<IPagedDataDto<DeliveryRunDto>> GetListAsync(int pageIndex, int pageSize, string? keywords,
        Guid? assignedDeliveryUserId, DeliveryRunStatus? status)
    {
        var query = runReader.DataSource;

        if (!string.IsNullOrWhiteSpace(keywords))
            query = query.Where(run => run.Code.Contains(keywords));
        if (assignedDeliveryUserId.HasValue)
            query = query.Where(run => run.AssignedDeliveryUserId == assignedDeliveryUserId.Value);
        if (status.HasValue)
            query = query.Where(run => run.Status == status.Value);

        query = query.OrderByDescending(run => run.CreatedOnUtc);

        var total = query.Count();
        if (total == 0)
            return Task.FromResult(PagedDataDto.Create(new List<DeliveryRunDto>(), pageIndex, pageSize, 0));

        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return Task.FromResult(PagedDataDto.Create(items.Select(run => run.ToDto()).ToList(), pageIndex, pageSize, total));
    }

    private static decimal GetCashCollectedAmount(DeliveryNote note)
        => note.DeliveryCashCollectedAmount ?? note.AmountToCollect;

    private async Task RecordCodPaymentsAsync(
        DeliveryRun run,
        IList<DeliveryNote> deliveredNotes,
        Guid? recordedByUserId,
        DateTime paidOnUtc,
        string? cashHandoverNote)
    {
        var itemOrder = run.Items
            .Select((item, index) => new { item.DeliveryNoteId, Index = index })
            .ToDictionary(item => item.DeliveryNoteId, item => item.Index);

        foreach (var note in deliveredNotes.OrderBy(note => itemOrder.GetValueOrDefault(note.Id)))
        {
            var amount = GetCashCollectedAmount(note);
            if (amount <= 0)
                continue;

            CustomerDebtDto? debt = null;
            if (note.AmountToCollect > 0)
            {
                debt = await customerDebtManager.CreateDebtFromDeliveryNoteAsync(new CreateCustomerDebtDto
                {
                    CustomerId = note.CustomerId,
                    DeliveryNoteId = note.Id,
                    TotalAmount = note.AmountToCollect
                }).ConfigureAwait(false);
            }

            var debtPaymentAmount = debt is null ? 0 : Math.Min(amount, debt.RemainingAmount);
            if (debt is not null && debtPaymentAmount > 0)
            {
                await customerDebtManager.RecordPaymentAsync(new CreateCustomerPaymentDto
                {
                    CustomerId = note.CustomerId,
                    OrderId = note.OrderId,
                    DeliveryNoteId = note.Id,
                    CustomerDebtId = debt.Id,
                    Amount = debtPaymentAmount,
                    PaymentMethod = PaymentMethod.Cash,
                    PaymentType = PaymentType.DebtPayment,
                    PaidOnUtc = paidOnUtc,
                    RecordedByUserId = recordedByUserId,
                    Note = BuildCodPaymentNote(run.Code, note.Code, cashHandoverNote)
                }).ConfigureAwait(false);
            }

            var extraAmount = amount - debtPaymentAmount;
            if (extraAmount > 0)
            {
                await customerDebtManager.RecordPaymentAsync(new CreateCustomerPaymentDto
                {
                    CustomerId = note.CustomerId,
                    OrderId = note.OrderId,
                    Amount = extraAmount,
                    PaymentMethod = PaymentMethod.Cash,
                    PaymentType = PaymentType.Deposit,
                    PaidOnUtc = paidOnUtc,
                    RecordedByUserId = recordedByUserId,
                    Note = BuildCodOverpaymentNote(run.Code, note.Code, cashHandoverNote)
                }).ConfigureAwait(false);
            }
        }
    }

    private static string BuildCodPaymentNote(string runCode, string noteCode, string? cashHandoverNote)
        => string.IsNullOrWhiteSpace(cashHandoverNote)
            ? $"Thu COD chuyen {runCode}, phieu {noteCode}"
            : $"Thu COD chuyen {runCode}, phieu {noteCode}. {cashHandoverNote.Trim()}";

    private static string BuildCodOverpaymentNote(string runCode, string noteCode, string? cashHandoverNote)
        => string.IsNullOrWhiteSpace(cashHandoverNote)
            ? $"Thu du COD chuyen {runCode}, phieu {noteCode}"
            : $"Thu du COD chuyen {runCode}, phieu {noteCode}. {cashHandoverNote.Trim()}";
}
