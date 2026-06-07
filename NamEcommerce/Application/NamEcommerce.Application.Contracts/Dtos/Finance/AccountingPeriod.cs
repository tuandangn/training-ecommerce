namespace NamEcommerce.Application.Contracts.Dtos.Finance;

public sealed record AccountingPeriod
{
    public int Year { get; init; }
    public int? Month { get; init; }
    public int? Quarter { get; init; }

    public DateTime Start => Month.HasValue
        ? new DateTime(Year, Month.Value, 1)
        : Quarter.HasValue
            ? new DateTime(Year, (Quarter.Value - 1) * 3 + 1, 1)
            : new DateTime(Year, 1, 1);

    public DateTime End => Month.HasValue
        ? Start.AddMonths(1).AddDays(-1)
        : Quarter.HasValue
            ? Start.AddMonths(3).AddDays(-1)
            : new DateTime(Year, 12, 31);

    public string Display => Month.HasValue ? $"Tháng {Month}/{Year}"
        : Quarter.HasValue ? $"Quý {Quarter}/{Year}"
        : $"Năm {Year}";

    public static AccountingPeriod ForMonth(int year, int month) => new() { Year = year, Month = month };
    public static AccountingPeriod ForQuarter(int year, int quarter) => new() { Year = year, Quarter = quarter };
    public static AccountingPeriod ForYear(int year) => new() { Year = year };
    public static AccountingPeriod Current() => ForMonth(DateTime.Today.Year, DateTime.Today.Month);
}
