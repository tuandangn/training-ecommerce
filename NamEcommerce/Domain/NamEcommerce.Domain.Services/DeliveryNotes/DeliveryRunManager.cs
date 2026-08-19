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
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace NamEcommerce.Domain.Services.DeliveryNotes;

public sealed class DeliveryRunManager(
    IRepository<DeliveryRun> runRepository,
    IRepository<DeliveryNote> deliveryNoteRepository,
    IEntityDataReader<DeliveryRun> runReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IDeliveryNoteManager deliveryNoteManager,
    ICustomerDebtManager customerDebtManager,
    ICurrentUserAccessor currentUserAccessor,
    EntityCodeGenerator entityCodeGenerator) : IDeliveryRunManager
{
    private Task<string> GenerateCodeAsync()
    {
        var prefix = $"CHG-{DateTime.UtcNow:yyyyMMdd}";
        return entityCodeGenerator.NextAsync(prefix, () => runReader.GetDataSource(new() { IncludeDeleted = true }).CountAsync(run => run.Code.StartsWith(prefix)));
    }

    public async Task<DeliveryRunDto> CreateAsync(CreateDeliveryRunDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var noteIds = dto.DeliveryNoteIds.Distinct().ToList();
        var activeNoteIds = await runReader.DataSource
            .Where(run => run.Status != DeliveryRunStatus.Closed && run.Status != DeliveryRunStatus.Cancelled)
            .SelectMany(run => run.Items)
            .Where(item => noteIds.Contains(item.DeliveryNoteId))
            .Select(item => item.DeliveryNoteId)
            .ToListAsync().ConfigureAwait(false);
        if (activeNoteIds.Count > 0)
            throw new NamEcommerceDomainException("Error.DeliveryRunDeliveryNoteAlreadyActive");

        var notes = await deliveryNoteReader.DataSource
            .Where(note => noteIds.Contains(note.Id))
            .ToListAsync().ConfigureAwait(false);
        if (notes.Count != noteIds.Count)
            throw new NamEcommerceDomainException("Error.DeliveryNoteNotFound");

        foreach (var note in notes)
        {
            if (note.Status != DeliveryNoteStatus.Confirmed)
                throw new NamEcommerceDomainException("Error.DeliveryRunDeliveryNoteMustBeConfirmed");
            if (note.IsDirectShip || note.SourceType != DeliveryNoteSourceType.ToCustomer)
                throw new NamEcommerceDomainException("Error.DeliveryRunDeliveryNoteSourceNotSupported");
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var run = new DeliveryRun(
            await GenerateCodeAsync().ConfigureAwait(false),
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
                string.IsNullOrWhiteSpace(note.ShippingPhoneNumber) ? note.CustomerInfo.PhoneNumber : note.ShippingPhoneNumber,
                note.ShippingAddress.Value,
                note.AmountToCollect);
        }

        foreach (var note in notes.Where(note => note.AssignedDeliveryUserId != dto.AssignedDeliveryUserId))
        {
            await deliveryNoteManager.AssignDeliveryUserAsync(new AssignDeliveryUserDto
            {
                DeliveryNoteId = note.Id,
                AssignedDeliveryUserId = dto.AssignedDeliveryUserId,
                AssignedDeliveryUsername = dto.AssignedDeliveryUsername,
                AssignedDeliveryFullName = dto.AssignedDeliveryFullName
            }).ConfigureAwait(false);
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

    public async Task ConfirmWarehousePickAsync(Guid id, Guid warehouseId)
    {
        var run = await runRepository.GetByIdAsync(id).ConfigureAwait(false)
                  ?? throw new NamEcommerceDomainException("Error.DeliveryRunNotFound");
        if (!run.DriverCachedOnUtc.HasValue && !run.PaperManifestIssued)
            throw new NamEcommerceDomainException("Error.DeliveryRunDriverNotAccepted");

        var warehouseIds = await GetRunWarehouseIdsAsync(run).ConfigureAwait(false);
        if (!warehouseIds.Contains(warehouseId))
            throw new NamEcommerceDomainException("Error.DeliveryRunWarehouseNotInRun");

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        run.ConfirmWarehousePick(warehouseId, currentUser?.Id, currentUser?.FullName, DateTime.UtcNow);
        await runRepository.UpdateAsync(run).ConfigureAwait(false);
    }

    public async Task HandOverAsync(Guid id)
    {
        var run = await runRepository.GetByIdAsync(id).ConfigureAwait(false)
                  ?? throw new NamEcommerceDomainException("Error.DeliveryRunNotFound");
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);

        var warehouseIds = await GetRunWarehouseIdsAsync(run).ConfigureAwait(false);
        run.HandOver(warehouseIds, currentUser?.Id, DateTime.UtcNow);
        foreach (var item in run.Items)
            await deliveryNoteManager.MarkDeliveringAsync(item.DeliveryNoteId).ConfigureAwait(false);

        await runRepository.UpdateAsync(run).ConfigureAwait(false);
    }

    private async Task<List<Guid>> GetRunWarehouseIdsAsync(DeliveryRun run)
    {
        var noteIds = run.Items.Select(item => item.DeliveryNoteId).ToList();
        return await deliveryNoteReader.DataSource
            .Where(note => noteIds.Contains(note.Id))
            .SelectMany(note => note.Items)
            .Select(item => item.WarehouseId)
            .Distinct()
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task CloseAsync(Guid id)
    {
        var run = await runRepository.GetByIdAsync(id).ConfigureAwait(false)
                  ?? throw new NamEcommerceDomainException("Error.DeliveryRunNotFound");

        var noteIds = run.Items.Select(item => item.DeliveryNoteId).ToList();
        var notes = await deliveryNoteReader.DataSource
            .Where(note => noteIds.Contains(note.Id))
            .ToListAsync().ConfigureAwait(false);

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
        if (run.Status == DeliveryRunStatus.Cancelled)
            throw new NamEcommerceDomainException("Error.DeliveryRunCannotConfirmCashHandover");

        var noteIds = run.Items.Select(item => item.DeliveryNoteId).ToList();
        var deliveredNotes = await deliveryNoteReader.DataSource
            .Where(note => noteIds.Contains(note.Id) && note.Status == DeliveryNoteStatus.Delivered)
            .ToListAsync()
            .ConfigureAwait(false);
        if (run.Status == DeliveryRunStatus.ReadyForHandover && deliveredNotes.Count == 0)
            throw new NamEcommerceDomainException("Error.DeliveryRunCannotConfirmCashHandover");

        var expectedAmount = deliveredNotes.Sum(GetCashCollectedAmount);
        if (expectedAmount <= 0)
            throw new NamEcommerceDomainException("Error.DeliveryRunCashHandoverNotRequired");
        if (Math.Abs(dto.Amount - expectedAmount) > 0.0001m)
            throw new NamEcommerceDomainException("Error.CashHandoverAmountMustMatchCollectedAmount");

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var confirmedOnUtc = DateTime.UtcNow;
        run.ConfirmCashHandover(currentUser?.Id, currentUser?.Username, currentUser?.FullName,
            dto.Amount, dto.Note, confirmedOnUtc);
        await RecordCodPaymentsAsync(run, deliveredNotes, currentUser?.Id, confirmedOnUtc, dto.Note)
            .ConfigureAwait(false);
        await runRepository.UpdateAsync(run).ConfigureAwait(false);
    }

    public async Task UpdateDeliveredNoteCashCollectedAsync(UpdateDeliveryRunDeliveredNoteCashCollectedDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var run = await runRepository.GetByIdAsync(dto.DeliveryRunId).ConfigureAwait(false)
                  ?? throw new NamEcommerceDomainException("Error.DeliveryRunNotFound");
        if (run.CashHandoverConfirmedOnUtc.HasValue)
            throw new NamEcommerceDomainException("Error.DeliveryRunCashHandoverAlreadyConfirmed");
        if (run.Items.All(item => item.DeliveryNoteId != dto.DeliveryNoteId))
            throw new NamEcommerceDomainException("Error.DeliveryRunDeliveryNoteNotInRun");

        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false)
                           ?? throw new NamEcommerceDomainException("Error.DeliveryNoteNotFound");

        deliveryNote.UpdateDeliveryCashCollectedAmount(dto.CashCollectedAmount);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
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

    public async Task<DeliveryRunDto?> GetByDeliveryNoteIdAsync(Guid deliveryNoteId)
    {
        var run = await runReader.DataSource
            .Where(r => r.Status != DeliveryRunStatus.Cancelled)
            .FirstOrDefaultAsync(r => r.Items.Any(item => item.DeliveryNoteId == deliveryNoteId))
            .ConfigureAwait(false);
        return run?.ToDto();
    }

    public async Task<IPagedDataDto<DeliveryRunDto>> GetListAsync(int pageIndex, int pageSize, string? keywords,
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

        var total = await query.CountAsync().ConfigureAwait(false);
        if (total == 0)
            return PagedDataDto.Create(new List<DeliveryRunDto>(), pageIndex, pageSize, 0);

        var items = await  query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync().ConfigureAwait(false);
        return PagedDataDto.Create(items.Select(run => run.ToDto()).ToList(), pageIndex, pageSize, total);
    }

    private static decimal GetCashCollectedAmount(DeliveryNote note)
    {
        if (!note.DeliveryCashCollectedAmount.HasValue)
        {
            if (note.AmountToCollect <= 0)
                return 0;

            throw new NamEcommerceDomainException("Error.DeliveryCashCollectedAmountRequired");
        }

        if (note.DeliveryCashCollectedAmount.Value > note.AmountToCollect)
            throw new NamEcommerceDomainException("Error.CashCollectedAmountCannotExceedAmountToCollect");

        return note.DeliveryCashCollectedAmount.Value;
    }

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
            var debtAmount = note.TotalAmount + note.Surcharge + (note.ApprovedAgreedCustomerCharge ?? 0m);
            if (debtAmount > 0)
            {
                debt = await customerDebtManager.CreateDebtFromDeliveryNoteAsync(new CreateCustomerDebtDto
                {
                    CustomerId = note.CustomerId,
                    DeliveryNoteId = note.Id,
                    TotalAmount = debtAmount
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

        }
    }

    private static string BuildCodPaymentNote(string runCode, string noteCode, string? cashHandoverNote)
        => string.IsNullOrWhiteSpace(cashHandoverNote)
            ? $"Thu COD chuyen {runCode}, phieu {noteCode}"
            : $"Thu COD chuyen {runCode}, phieu {noteCode}. {cashHandoverNote.Trim()}";

}
