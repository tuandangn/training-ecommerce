namespace NamEcommerce.Domain.Shared.Exceptions.StockAdjustment;

public sealed class StockAdjustmentNoteNotFoundException(Guid id)
    : NamEcommerceDomainException($"StockAdjustmentNote with id '{id}' was not found.");

public sealed class StockAdjustmentNoteCannotChangeStatusException(string from, string to)
    : NamEcommerceDomainException($"Cannot change StockAdjustmentNote status from '{from}' to '{to}'.");
