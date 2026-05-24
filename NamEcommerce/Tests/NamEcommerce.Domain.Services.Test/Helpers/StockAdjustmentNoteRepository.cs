using NamEcommerce.Domain.Entities.StockAdjustment;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class StockAdjustmentNoteRepository
{
    public static Mock<IRepository<StockAdjustmentNote>> Create()
        => Repository.Create<StockAdjustmentNote>();

    /// <summary>
    /// Setup mock cho phép InsertAsync và trả lại entity đã insert.
    /// </summary>
    public static Mock<IRepository<StockAdjustmentNote>> ExpectsInsert()
    {
        var mock = Create();
        mock.Setup(r => r.InsertAsync(It.IsAny<StockAdjustmentNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockAdjustmentNote n, CancellationToken _) => n)
            .Verifiable();
        return mock;
    }

    /// <summary>
    /// Setup mock cho phép verify rằng UpdateAsync được gọi đúng 1 lần.
    /// </summary>
    public static Mock<IRepository<StockAdjustmentNote>> ExpectsUpdate()
    {
        var mock = Create();
        mock.Setup(r => r.UpdateAsync(It.IsAny<StockAdjustmentNote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockAdjustmentNote n, CancellationToken _) => n)
            .Verifiable();
        return mock;
    }

    /// <summary>
    /// Setup mock GetByIdAsync trả về note.
    /// </summary>
    public static Mock<IRepository<StockAdjustmentNote>> NoteById(Guid id, StockAdjustmentNote note)
        => Repository.Create<StockAdjustmentNote>().WhenCall(r => r.GetByIdAsync(id, default), note);

    /// <summary>
    /// Setup mock GetByIdAsync trả về null (not found).
    /// </summary>
    public static Mock<IRepository<StockAdjustmentNote>> NotFound(Guid id)
        => Repository.Create<StockAdjustmentNote>().WhenCall(r => r.GetByIdAsync(id, default), (StockAdjustmentNote?)null);
}
