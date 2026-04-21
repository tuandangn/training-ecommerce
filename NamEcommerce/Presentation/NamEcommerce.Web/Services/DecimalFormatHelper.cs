using System.Globalization;
using System.Text;

namespace NamEcommerce.Web.Services;

/// <summary>
/// Format decimal để hiển thị trong View — đồng bộ với decimal-fields.js và so-bang-chu.js.
/// Không phụ thuộc culture của server hay browser.
/// </summary>
public static class DecimalFormatHelper
{
    // ── Số → chuỗi có format ─────────────────────────────────

    /// <summary>
    /// Giá tiền: 3400000 → "3.400.000"
    /// </summary>
    public static string FormatCurrency(decimal? value)
    {
        if (!value.HasValue) return string.Empty;
        return InsertThousandSeparator(((long)value.Value).ToString(), '.', 'đ');
    }

    /// <summary>
    /// Số lượng: 1234.56 → "1.234,56"
    /// </summary>
    public static string FormatQuantity(decimal? value, int decimalPlaces = 2)
    {
        if (!value.HasValue)
            return string.Empty;

        var raw = Math.Round(value.Value, decimalPlaces).ToString("F" + decimalPlaces, CultureInfo.InvariantCulture);
        var parts = raw.Split('.');
        var intPart = InsertThousandSeparator(parts[0], '.');
        if (parts.Length == 1)
            return intPart;

        var fracPart = parts[1];
        if (fracPart == "00")
            return intPart;

        return $"{intPart},{fracPart}";
    }

    // ── private: format ──────────────────────────────────────

    private static string InsertThousandSeparator(string intStr, char sep, char? endSymbol = null)
    {
        var sb = new StringBuilder();
        int count = 0;

        for (int i = intStr.Length - 1; i >= 0; i--)
        {
            if (count > 0 && count % 3 == 0)
                sb.Insert(0, sep);
            sb.Insert(0, intStr[i]);
            count++;
        }
        if (endSymbol.HasValue)
            sb.AppendFormat(" {0}", endSymbol.Value);

        return sb.ToString();
    }

    // ── Số → chữ Tiếng Việt ──────────────────────────────────

    /// <summary>
    /// Đọc số tiền bằng chữ Tiếng Việt — đồng bộ với SoBangChu.docSoTien() trong JS.
    /// 3_400_000 → "Ba triệu bốn trăm nghìn đồng"
    /// </summary>
    public static string ToVietnameseCurrencyHint(decimal? value, string donViTien = "đồng")
    {
        if (!value.HasValue || value.Value == 0) return string.Empty;

        var n = (long)Math.Abs(value.Value);
        var chu = DocSo(n);

        if (string.IsNullOrEmpty(chu)) return string.Empty;

        var prefix = value.Value < 0 ? "âm " : string.Empty;
        return prefix + char.ToUpper(chu[0]) + chu[1..] + " " + donViTien;
    }

    // ── private: đọc số ──────────────────────────────────────

    private static readonly string[] DonVi =
        ["", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"];

    private static readonly string[] Hang =
        ["", "nghìn", "triệu", "tỷ"];

    private static string DocSo(long n)
    {
        if (n == 0) return "không";

        // Tách thành nhóm 3 chữ số từ phải sang trái
        var nhoms = new List<int>();
        var tmp = n;
        while (tmp > 0)
        {
            nhoms.Insert(0, (int)(tmp % 1000));
            tmp /= 1000;
        }

        var parts = new List<string>();
        int offset = nhoms.Count - 1;

        for (int i = 0; i < nhoms.Count; i++)
        {
            var nhom = nhoms[i];
            if (nhom == 0) continue;

            int hang = offset - i;
            int hangIdx = hang % 4;

            var chu = DocNhom(nhom, i == 0);
            var suffix = hang >= 4 && hangIdx == 0 ? "tỷ" : Hang[hangIdx];

            parts.Add(string.IsNullOrEmpty(suffix) ? chu : chu + " " + suffix);
        }

        return string.Join(" ", parts);
    }

    /// <summary>Đọc nhóm 3 chữ số (0–999).</summary>
    private static string DocNhom(int n, bool isFirst)
    {
        if (n == 0) return string.Empty;

        int tram = n / 100;
        int chuc = (n % 100) / 10;
        int donvi = n % 10;

        var sb = new StringBuilder();

        // Hàng trăm
        if (tram > 0)
            sb.Append(DonVi[tram]).Append(" trăm");
        else if (!isFirst)
            sb.Append("không trăm");

        // Hàng chục
        if (chuc == 0)
        {
            if (donvi > 0)
                sb.Append(" lẻ ").Append(DonVi[donvi]);
        }
        else if (chuc == 1)
        {
            sb.Append(" mười");
            if (donvi == 5) sb.Append(" lăm");
            else if (donvi > 0) sb.Append(' ').Append(DonVi[donvi]);
        }
        else
        {
            sb.Append(' ').Append(DonVi[chuc]).Append(" mươi");
            if (donvi == 1) sb.Append(" mốt");
            else if (donvi == 5) sb.Append(" lăm");
            else if (donvi > 0) sb.Append(' ').Append(DonVi[donvi]);
        }

        return sb.ToString().Trim();
    }
}