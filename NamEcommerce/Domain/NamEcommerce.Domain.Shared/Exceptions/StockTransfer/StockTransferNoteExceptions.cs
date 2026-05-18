namespace NamEcommerce.Domain.Shared.Exceptions.StockTransfer;

public sealed class StockTransferNoteNotFoundException(Guid id)
    : NamEcommerceDomainException($"StockTransferNote with id '{id}' was not found.");

public sealed class StockTransferNoteCannotChangeStatusException(string from, string to)
    : NamEcommerceDomainException($"Cannot change StockTransferNote status from '{from}' to '{to}'.");

public sealed class StockTransferSameWarehouseException()
    : NamEcommerceDomainException("Error.StockTransfer.SameWarehouse");

public sealed class StockTransferInsufficientStockException(string productName, decimal available, decimal requested)
    : NamEcommerceDomainException($"Error.StockTransfer.InsufficientStock: '{productName}' available={available}, requested={requested}");
