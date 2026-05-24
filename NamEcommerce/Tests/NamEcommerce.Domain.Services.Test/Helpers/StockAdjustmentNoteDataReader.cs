using NamEcommerce.Domain.Entities.StockAdjustment;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class StockAdjustmentNoteDataReader
{
    public static Mock<IEntityDataReader<StockAdjustmentNote>> Empty()
        => EntityDataReader.Create<StockAdjustmentNote>().WithData(Array.Empty<StockAdjustmentNote>());

    public static Mock<IEntityDataReader<StockAdjustmentNote>> HasOne(StockAdjustmentNote note)
        => EntityDataReader.Create<StockAdjustmentNote>().WithData(note);

    public static Mock<IEntityDataReader<StockAdjustmentNote>> NoteById(Guid id, StockAdjustmentNote note)
        => EntityDataReader.Create<StockAdjustmentNote>().WhenCall(r => r.GetByIdAsync(id), note);

    public static Mock<IEntityDataReader<StockAdjustmentNote>> NotFound(Guid id)
        => EntityDataReader.Create<StockAdjustmentNote>().WhenCall(r => r.GetByIdAsync(id), (StockAdjustmentNote?)null);
}
