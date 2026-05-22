namespace NamEcommerce.Domain.Shared.Exceptions.StockTransfer;

public sealed class StockTransferNoteNotFoundException(Guid id)
    : NamEcommerceDomainException($"StockTransferNote was not found.");

public sealed class StockTransferNoteCannotChangeStatusException(string from, string to)
    : NamEcommerceDomainException($"Cannot change StockTransferNote status.");

public sealed class StockTransferSameWarehouseException()
    : NamEcommerceDomainException("Error.StockTransfer.SameWarehouse");

public sealed class StockTransferInsufficientStockException(string productName, decimal available, decimal requested)
    : NamEcommerceDomainException($"Error.StockTransfer.InsufficientStock: '{productName}' available={available}, requested={requested}");
