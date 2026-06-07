using ClosedXML.Excel;
using NamEcommerce.Application.Contracts.Dtos.Finance;

namespace NamEcommerce.Web.Helpers;

public static class AccountingExcelExporter
{
    public static byte[] ExportIncomeStatement(IncomeStatementDto dto)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("B02");

        ws.Cell(1, 1).Value = "BÁO CÁO KẾT QUẢ HOẠT ĐỘNG KINH DOANH";
        ws.Cell(2, 1).Value = dto.Period;
        ws.Range(1, 1, 1, 3).Merge().Style.Font.SetBold(true).Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Range(2, 1, 2, 3).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        var headers = new[] { "Chỉ tiêu", "Kỳ này (đ)", "Ghi chú" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(4, i + 1).Value = headers[i];
            ws.Cell(4, i + 1).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.LightGray);
        }

        var rows = new (string label, decimal value, bool bold)[]
        {
            ("[01] Doanh thu gộp", dto.GrossRevenue, false),
            ("  Chiết khấu thương mại (TK 521)", -dto.TradeDiscounts, false),
            ("  Hàng bán bị trả lại (TK 531)", -dto.SalesReturns, false),
            ("[10] Doanh thu thuần", dto.NetRevenue, true),
            ("[11] Giá vốn hàng bán", -dto.NetCostOfGoodsSold, false),
            ("[20] Lợi nhuận gộp", dto.GrossProfit, true),
            ("[25] Chi phí bán hàng", -dto.TotalSellingExpenses, false),
            ("[26] Chi phí QLDN", -dto.TotalAdminExpenses, false),
            ("[30] Lợi nhuận thuần từ HĐKD", dto.OperatingProfit, true),
            ("[50] Lợi nhuận trước thuế", dto.ProfitBeforeTax, true),
            ("[51] Thuế TNDN (TK 821)", -dto.CorporateTax, false),
            ("[60] Lợi nhuận sau thuế", dto.NetProfit, true),
        };

        for (int i = 0; i < rows.Length; i++)
        {
            var row = i + 5;
            ws.Cell(row, 1).Value = rows[i].label;
            ws.Cell(row, 2).Value = rows[i].value;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            if (rows[i].bold)
                ws.Row(row).Style.Font.SetBold(true);
        }

        ws.Column(1).Width = 45;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 20;

        return ToBytes(wb);
    }

    public static byte[] ExportCashFlow(CashFlowStatementDto dto)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("B03");

        ws.Cell(1, 1).Value = "BÁO CÁO LƯU CHUYỂN TIỀN TỆ (Phương pháp gián tiếp)";
        ws.Cell(2, 1).Value = dto.Period;
        ws.Range(1, 1, 1, 2).Merge().Style.Font.SetBold(true).Font.SetFontSize(13).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Range(2, 1, 2, 2).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        ws.Cell(4, 1).Value = "Chỉ tiêu";
        ws.Cell(4, 2).Value = "Số tiền (đ)";
        ws.Row(4).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.LightGray);

        var rows = new (string label, decimal value, bool bold, bool isSection)[]
        {
            ("I. LƯU CHUYỂN TIỀN TỪ HĐKD", 0, true, true),
            ("  Lợi nhuận trước thuế", dto.ProfitBeforeTax, false, false),
            ("  Khấu hao TSCĐ (cộng lại)", dto.DepreciationAdjustment, false, false),
            ("  Thay đổi phải thu KH (TK 131)", dto.AccountsReceivableChange, false, false),
            ("  Thay đổi phải trả NCC (TK 331)", dto.AccountsPayableChange, false, false),
            ("  Thay đổi hàng tồn kho (TK 156)", dto.InventoryChange, false, false),
            ("  Thay đổi thuế GTGT phải nộp", dto.VatPayableChange, false, false),
            ("  Hoàn tiền khách hàng (tiền ra)", dto.CustomerRefundsOut, false, false),
            ("  Lưu chuyển thuần từ HĐKD", dto.OperatingCashFlow, true, false),
            ("II. LƯU CHUYỂN TIỀN TỪ HĐ ĐẦU TƯ", 0, true, true),
            ("  Mua sắm TSCĐ", dto.FixedAssetPurchases, false, false),
            ("  Lưu chuyển thuần từ HĐĐT", dto.InvestingCashFlow, true, false),
            ("III. LƯU CHUYỂN TIỀN TỪ HĐ TÀI CHÍNH = 0", 0, false, false),
            ("IV. Tăng/giảm tiền thuần trong kỳ", dto.NetCashChange, true, false),
            ("V. Tiền đầu kỳ", dto.OpeningCash, false, false),
            ("VI. Tiền cuối kỳ (theo B03)", dto.ClosingCash, true, false),
            ("   Tiền cuối kỳ (theo sổ quỹ)", dto.ActualClosingCash, false, false),
            ("   Chênh lệch", dto.ClosingCash - dto.ActualClosingCash, false, false),
        };

        for (int i = 0; i < rows.Length; i++)
        {
            var row = i + 5;
            ws.Cell(row, 1).Value = rows[i].label;
            if (!rows[i].isSection)
            {
                ws.Cell(row, 2).Value = rows[i].value;
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            }
            if (rows[i].bold) ws.Row(row).Style.Font.SetBold(true);
            if (rows[i].isSection) ws.Row(row).Style.Fill.SetBackgroundColor(XLColor.LightBlue);
        }

        ws.Column(1).Width = 50;
        ws.Column(2).Width = 20;

        return ToBytes(wb);
    }

    public static byte[] ExportBalanceSheet(BalanceSheetDto dto)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("B01");

        ws.Cell(1, 1).Value = "BẢNG CÂN ĐỐI KẾ TOÁN";
        ws.Cell(2, 1).Value = $"Ngày {dto.AsOf:dd/MM/yyyy}";
        ws.Range(1, 1, 1, 3).Merge().Style.Font.SetBold(true).Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Range(2, 1, 2, 3).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        ws.Cell(4, 1).Value = "Chỉ tiêu";
        ws.Cell(4, 2).Value = "Cuối kỳ (đ)";
        ws.Cell(4, 3).Value = "Đầu kỳ (đ)";
        ws.Row(4).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.LightGray);

        var prior = dto.PriorPeriod;

        var rows = new (string label, decimal val, decimal? priorVal, bool bold, bool section)[]
        {
            ("A. TÀI SẢN NGẮN HẠN", dto.TotalCurrentAssets, prior?.TotalCurrentAssets, true, true),
            ("  I. Tiền và tương đương tiền", dto.TotalCash, prior?.TotalCash, false, false),
            ("    Tiền mặt (TK 111)", dto.CashOnHand, prior?.CashOnHand, false, false),
            ("    Tiền gửi ngân hàng (TK 112)", dto.TotalBankDeposits, prior?.TotalBankDeposits, false, false),
            ("  II. Phải thu ngắn hạn (TK 131)", dto.TradeReceivables, prior?.TradeReceivables, false, false),
            ("  III. Hàng tồn kho (TK 156)", dto.Inventory, prior?.Inventory, false, false),
            ("B. TÀI SẢN DÀI HẠN", dto.TotalNonCurrentAssets, prior?.TotalNonCurrentAssets, true, true),
            ("  II. TSCĐ — Nguyên giá (TK 211)", dto.FixedAssetsGross, prior?.FixedAssetsGross, false, false),
            ("       Khấu hao lũy kế (TK 214)", -dto.AccumulatedDepreciation, prior != null ? (decimal?)-prior.AccumulatedDepreciation : null, false, false),
            ("       Giá trị còn lại", dto.FixedAssetsNet, prior?.FixedAssetsNet, false, false),
            ("TỔNG TÀI SẢN", dto.TotalAssets, prior?.TotalAssets, true, true),
            ("A. NỢ PHẢI TRẢ", dto.TotalLiabilities, prior?.TotalLiabilities, true, true),
            ("  I. Nợ ngắn hạn", dto.TotalCurrentLiabilities, prior?.TotalCurrentLiabilities, false, false),
            ("    Phải trả NCC (TK 331)", dto.TradePayables, prior?.TradePayables, false, false),
            ("    Thuế GTGT phải nộp (TK 3331)", dto.VatPayable, prior?.VatPayable, false, false),
            ("    Thuế TNDN (TK 3334)", dto.CorporateTaxPayable, prior?.CorporateTaxPayable, false, false),
            ("B. VỐN CHỦ SỞ HỮU", dto.TotalEquity, prior?.TotalEquity, true, true),
            ("    Vốn góp (TK 411)", dto.PaidInCapital, prior?.PaidInCapital, false, false),
            ("    LNST chưa phân phối (TK 421)", dto.RetainedEarnings, prior?.RetainedEarnings, false, false),
            ("TỔNG NGUỒN VỐN", dto.TotalLiabilitiesAndEquity, prior?.TotalLiabilitiesAndEquity, true, true),
        };

        for (int i = 0; i < rows.Length; i++)
        {
            var row = i + 5;
            ws.Cell(row, 1).Value = rows[i].label;
            ws.Cell(row, 2).Value = rows[i].val;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            if (rows[i].priorVal.HasValue)
            {
                ws.Cell(row, 3).Value = rows[i].priorVal!.Value;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            }
            if (rows[i].bold) ws.Row(row).Style.Font.SetBold(true);
            if (rows[i].section) ws.Row(row).Style.Fill.SetBackgroundColor(XLColor.LightBlue);
        }

        ws.Column(1).Width = 45;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 20;

        return ToBytes(wb);
    }

    public static byte[] ExportCashBook(CashBookDto dto)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sổ quỹ");

        ws.Cell(1, 1).Value = "SỔ QUỸ TIỀN MẶT";
        ws.Cell(2, 1).Value = dto.Period;
        ws.Range(1, 1, 1, 5).Merge().Style.Font.SetBold(true).Font.SetFontSize(13).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Range(2, 1, 2, 5).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        ws.Cell(4, 1).Value = "Ngày";
        ws.Cell(4, 2).Value = "Diễn giải";
        ws.Cell(4, 3).Value = "Tiền vào (đ)";
        ws.Cell(4, 4).Value = "Tiền ra (đ)";
        ws.Cell(4, 5).Value = "Số dư (đ)";
        ws.Row(4).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.LightGray);

        ws.Cell(5, 1).Value = "Số dư đầu kỳ";
        ws.Cell(5, 5).Value = dto.OpeningBalance;
        ws.Cell(5, 5).Style.NumberFormat.Format = "#,##0";
        ws.Row(5).Style.Font.SetBold(true);

        int row = 6;
        foreach (var line in dto.Lines)
        {
            ws.Cell(row, 1).Value = line.Date.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = line.Description;
            if (line.AmountIn > 0) { ws.Cell(row, 3).Value = line.AmountIn; ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0"; }
            if (line.AmountOut > 0) { ws.Cell(row, 4).Value = line.AmountOut; ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0"; }
            ws.Cell(row, 5).Value = line.RunningBalance;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        ws.Cell(row, 1).Value = "Số dư cuối kỳ";
        ws.Cell(row, 3).Value = dto.TotalIn;
        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 4).Value = dto.TotalOut;
        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 5).Value = dto.ClosingBalance;
        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
        ws.Row(row).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.LightGray);

        ws.Column(1).Width = 14;
        ws.Column(2).Width = 45;
        ws.Column(3).Width = 18;
        ws.Column(4).Width = 18;
        ws.Column(5).Width = 18;

        return ToBytes(wb);
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
