namespace NamEcommerce.Domain.Specifications.Filters;

[Serializable]
public sealed class DateRangeFilter
{
    private readonly DateTime? _fromDate;
    private readonly DateTime? _toDate;

    public DateRangeFilter(DateTime? fromDate, DateTime? toDate)
        => (_fromDate, _toDate) = (fromDate, toDate);

    public DateTime? FromDate => _fromDate?.Date;
    public DateTime? ToDate => _toDate?.Date.AddDays(1).AddMilliseconds(-1);

    public static implicit operator DateRangeFilter((DateTime? fromDate, DateTime? toDate) dateRange)
        => new DateRangeFilter(dateRange.fromDate, dateRange.toDate);
}
