using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Report;
using ClosedXML.Excel;

namespace NamEcommerce.Web.Controllers;

public sealed class ReportController : BaseAuthorizedController
{
    private readonly IFinancialReportAppService _reportAppService;

    public ReportController(IFinancialReportAppService reportAppService)
    {
        _reportAppService = reportAppService;
    }

    [HttpGet]
    public async Task<IActionResult> Financial(DateTime? fromDate, DateTime? toDate)
    {
        // Default to last 30 days if not specified
        if (!fromDate.HasValue) fromDate = DateTime.Today.AddDays(-30);
        if (!toDate.HasValue) toDate = DateTime.Today;

        var model = await _reportAppService.GetProfitLossSummaryAsync(fromDate, toDate);
        
        ViewData["FromDate"] = fromDate.Value.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate.Value.ToString("yyyy-MM-dd");
        
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportIncomeStatementExcel(DateTime? fromDate, DateTime? toDate)
    {
        if (!fromDate.HasValue) fromDate = DateTime.Today.AddDays(-30);
        if (!toDate.HasValue) toDate = DateTime.Today;

        var model = await _reportAppService.GetProfitLossSummaryAsync(fromDate, toDate);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("BÁO CÁO KQ KINH DOANH");

        worksheet.Cell("A1").Value = "CÔNG TY TNHH NAM ECOMMERCE";
        worksheet.Cell("A1").Style.Font.Bold = true;
        
        worksheet.Cell("A3").Value = "BÁO CÁO KẾT QUẢ HOẠT ĐỘNG KINH DOANH";
        worksheet.Cell("A3").Style.Font.Bold = true;
        worksheet.Cell("A3").Style.Font.FontSize = 14;
        worksheet.Range("A3:E3").Merge();
        worksheet.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var periodText = $"Kỳ báo cáo: Từ {fromDate.Value:dd/MM/yyyy} đến {toDate.Value:dd/MM/yyyy}";
        worksheet.Cell("A4").Value = periodText;
        worksheet.Cell("A4").Style.Font.Italic = true;
        worksheet.Range("A4:E4").Merge();
        worksheet.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var row = 6;
        worksheet.Cell(row, 1).Value = "Chỉ tiêu";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Range(row, 1, row, 3).Merge();
        worksheet.Cell(row, 4).Value = "Mã số";
        worksheet.Cell(row, 4).Style.Font.Bold = true;
        worksheet.Cell(row, 5).Value = "Số tiền (VNĐ)";
        worksheet.Cell(row, 5).Style.Font.Bold = true;

        row++;
        worksheet.Range(row, 1, row, 3).Merge().Value = "1. Doanh thu bán hàng và cung cấp dịch vụ";
        worksheet.Cell(row, 4).Value = "01";
        worksheet.Cell(row, 5).Value = model.TotalRevenue;
        
        row++;
        worksheet.Range(row, 1, row, 3).Merge().Value = "2. Các khoản giảm trừ doanh thu";
        worksheet.Cell(row, 4).Value = "02";
        worksheet.Cell(row, 5).Value = 0;

        row++;
        worksheet.Range(row, 1, row, 3).Merge().Value = "3. Doanh thu thuần về bán hàng và cung cấp dịch vụ (10 = 01 - 02)";
        worksheet.Cell(row, 4).Value = "10";
        worksheet.Cell(row, 5).Value = model.TotalRevenue;
        worksheet.Cell(row, 1).Style.Font.Bold = true;

        row++;
        worksheet.Range(row, 1, row, 3).Merge().Value = "4. Giá vốn hàng bán";
        worksheet.Cell(row, 4).Value = "11";
        worksheet.Cell(row, 5).Value = model.TotalCogs;

        row++;
        worksheet.Range(row, 1, row, 3).Merge().Value = "5. Lợi nhuận gộp về bán hàng và cung cấp dịch vụ (20 = 10 - 11)";
        worksheet.Cell(row, 4).Value = "20";
        worksheet.Cell(row, 5).Value = model.GrossProfit;
        worksheet.Cell(row, 1).Style.Font.Bold = true;

        row++;
        worksheet.Range(row, 1, row, 3).Merge().Value = "6. Chi phí hoạt động kinh doanh (Bán hàng, CT Khác)";
        worksheet.Cell(row, 4).Value = "25";
        worksheet.Cell(row, 5).Value = model.TotalOperatingExpenses;

        row++;
        worksheet.Range(row, 1, row, 3).Merge().Value = "7. Lợi nhuận thuần từ hoạt động kinh doanh (30 = 20 - 25)";
        worksheet.Cell(row, 4).Value = "30";
        worksheet.Cell(row, 5).Value = model.NetProfit;
        worksheet.Row(row).Style.Font.Bold = true;

        worksheet.Range($"E7:E{row}").Style.NumberFormat.Format = "#,##0";
        var rngTable = worksheet.Range($"A6:E{row}");
        rngTable.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        rngTable.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        row += 3;
        worksheet.Cell(row, 4).Value = $"Lập, ngày {DateTime.Now.Day:00} tháng {DateTime.Now.Month:00} năm {DateTime.Now.Year}";
        worksheet.Cell(row, 4).Style.Font.Italic = true;
        worksheet.Range(row, 4, row, 5).Merge();
        worksheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        row++;
        worksheet.Range(row, 1, row, 2).Merge().Value = "Người lập biểu";
        worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
        worksheet.Range(row, 1, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(row, 4, row, 5).Merge().Value = "Giám đốc / Kế toán trưởng";
        worksheet.Range(row, 4, row, 5).Style.Font.Bold = true;
        worksheet.Range(row, 4, row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        row++;
        worksheet.Range(row, 1, row, 2).Merge().Value = "(Ký, họ tên)";
        worksheet.Range(row, 1, row, 2).Style.Font.Italic = true;
        worksheet.Range(row, 1, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(row, 4, row, 5).Merge().Value = "(Ký, họ tên, đóng dấu)";
        worksheet.Range(row, 4, row, 5).Style.Font.Italic = true;
        worksheet.Range(row, 4, row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"BCTC_B02_DN_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
