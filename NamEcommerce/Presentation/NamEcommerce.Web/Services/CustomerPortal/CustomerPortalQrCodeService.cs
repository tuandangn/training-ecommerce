using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NamEcommerce.Application.Contracts.CustomerPortal;

namespace NamEcommerce.Web.Services.CustomerPortal;

public sealed class CustomerPortalQrCodeService(
    ICustomerPortalDeliveryTokenAppService tokenAppService,
    IConfiguration configuration) : ICustomerPortalQrCodeService
{
    public async Task<CustomerPortalDeliveryQrCodeModel?> CreateDeliveryQrCodeAsync(Guid deliveryNoteId, HttpRequest request)
    {
        var token = await tokenAppService.CreateDeliveryAccessTokenAsync(deliveryNoteId).ConfigureAwait(false);
        if (token is null)
            return null;

        var url = $"{GetClientBaseUrl(request)}/d/{token.Token}";
        return new CustomerPortalDeliveryQrCodeModel(url, QrCodeSvgRenderer.Render(url));
    }

    private string GetClientBaseUrl(HttpRequest request)
    {
        var configured = configuration["CustomerPortal:ClientBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');

        return $"{request.Scheme}://{request.Host}".TrimEnd('/');
    }
}

internal static class QrCodeSvgRenderer
{
    public static string Render(string text)
    {
        var matrix = QrCodeGenerator.Encode(text);
        const int border = 4;
        var size = matrix.GetLength(0);
        var dimension = size + border * 2;
        var builder = new StringBuilder();

        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
            .Append(dimension)
            .Append(' ')
            .Append(dimension)
            .Append("\" shape-rendering=\"crispEdges\" aria-hidden=\"true\">");
        builder.Append("<rect width=\"100%\" height=\"100%\" fill=\"#fff\"/>");
        builder.Append("<path fill=\"#000\" d=\"");

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (!matrix[x, y])
                    continue;

                builder.Append('M')
                    .Append(x + border)
                    .Append(' ')
                    .Append(y + border)
                    .Append("h1v1h-1z");
            }
        }

        builder.Append("\"/></svg>");
        return builder.ToString();
    }
}

internal static class QrCodeGenerator
{
    private const int Version = 10;
    private const int Size = Version * 4 + 17;
    private const int DataCodewordCount = 274;
    private const int ErrorCorrectionCodewordCount = 18;
    private static readonly int[] DataBlockLengths = [68, 68, 69, 69];

    public static bool[,] Encode(string text)
    {
        var data = Encoding.UTF8.GetBytes(text);
        var codewords = MakeCodewords(data);

        bool[,]? best = null;
        var bestPenalty = int.MaxValue;

        for (var mask = 0; mask < 8; mask++)
        {
            var matrix = BuildMatrix(codewords, mask);
            var penalty = CalculatePenalty(matrix);
            if (penalty >= bestPenalty)
                continue;

            best = matrix;
            bestPenalty = penalty;
        }

        return best ?? throw new InvalidOperationException("Unable to generate QR code.");
    }

    private static bool[,] BuildMatrix(byte[] codewords, int mask)
    {
        var qr = new QrMatrix(Size);
        qr.DrawFunctionPatterns(Version);
        qr.DrawCodewords(codewords);
        qr.ApplyMask(mask);
        qr.DrawFormatBits(mask);
        qr.DrawVersionBits(Version);
        return qr.Modules;
    }

    private static byte[] MakeCodewords(byte[] data)
    {
        var dataCodewords = MakeDataCodewords(data);
        var blocks = new List<byte[]>();
        var offset = 0;

        foreach (var length in DataBlockLengths)
        {
            blocks.Add(dataCodewords.Skip(offset).Take(length).ToArray());
            offset += length;
        }

        var errorBlocks = blocks
            .Select(block => ReedSolomon.ComputeRemainder(block, ErrorCorrectionCodewordCount))
            .ToList();

        var result = new List<byte>();
        var maxDataLength = blocks.Max(block => block.Length);
        for (var i = 0; i < maxDataLength; i++)
        {
            foreach (var block in blocks)
            {
                if (i < block.Length)
                    result.Add(block[i]);
            }
        }

        for (var i = 0; i < ErrorCorrectionCodewordCount; i++)
        {
            foreach (var block in errorBlocks)
                result.Add(block[i]);
        }

        return result.ToArray();
    }

    private static byte[] MakeDataCodewords(byte[] data)
    {
        var bits = new List<int>();
        AppendBits(bits, 0b0100, 4);
        AppendBits(bits, data.Length, 16);

        foreach (var value in data)
            AppendBits(bits, value, 8);

        var capacityBits = DataCodewordCount * 8;
        if (bits.Count > capacityBits)
            throw new InvalidOperationException("Customer portal QR URL is too long.");

        var terminatorLength = Math.Min(4, capacityBits - bits.Count);
        AppendBits(bits, 0, terminatorLength);
        while (bits.Count % 8 != 0)
            bits.Add(0);

        var result = new List<byte>();
        for (var i = 0; i < bits.Count; i += 8)
        {
            var value = 0;
            for (var j = 0; j < 8; j++)
                value = (value << 1) | bits[i + j];
            result.Add((byte)value);
        }

        for (var pad = 0; result.Count < DataCodewordCount; pad++)
            result.Add((byte)(pad % 2 == 0 ? 0xEC : 0x11));

        return result.ToArray();
    }

    private static void AppendBits(ICollection<int> bits, int value, int length)
    {
        for (var i = length - 1; i >= 0; i--)
            bits.Add((value >> i) & 1);
    }

    private static int CalculatePenalty(bool[,] modules)
    {
        var penalty = 0;
        var size = modules.GetLength(0);

        for (var y = 0; y < size; y++)
            penalty += CountRunPenalty(modules, true, y);
        for (var x = 0; x < size; x++)
            penalty += CountRunPenalty(modules, false, x);

        for (var y = 0; y < size - 1; y++)
        {
            for (var x = 0; x < size - 1; x++)
            {
                var color = modules[x, y];
                if (color == modules[x + 1, y] && color == modules[x, y + 1] && color == modules[x + 1, y + 1])
                    penalty += 3;
            }
        }

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size - 6; x++)
            {
                if (HasFinderLikePattern(modules, x, y, true))
                    penalty += 40;
            }
        }

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size - 6; y++)
            {
                if (HasFinderLikePattern(modules, x, y, false))
                    penalty += 40;
            }
        }

        var dark = 0;
        foreach (var module in modules)
        {
            if (module)
                dark++;
        }

        var total = size * size;
        var k = Math.Abs(dark * 20 - total * 10) / total;
        return penalty + k * 10;
    }

    private static int CountRunPenalty(bool[,] modules, bool row, int index)
    {
        var size = modules.GetLength(0);
        var penalty = 0;
        var runColor = row ? modules[0, index] : modules[index, 0];
        var runLength = 1;

        for (var i = 1; i < size; i++)
        {
            var color = row ? modules[i, index] : modules[index, i];
            if (color == runColor)
            {
                runLength++;
                continue;
            }

            if (runLength >= 5)
                penalty += runLength == 5 ? 3 : runLength - 2;

            runColor = color;
            runLength = 1;
        }

        if (runLength >= 5)
            penalty += runLength == 5 ? 3 : runLength - 2;

        return penalty;
    }

    private static bool HasFinderLikePattern(bool[,] modules, int x, int y, bool row)
    {
        var pattern = new[] { true, false, true, true, true, false, true };
        for (var i = 0; i < pattern.Length; i++)
        {
            var color = row ? modules[x + i, y] : modules[x, y + i];
            if (color != pattern[i])
                return false;
        }

        return HasLightRun(modules, x - 4, y, 4, row) || HasLightRun(modules, x + 7, y, 4, row);
    }

    private static bool HasLightRun(bool[,] modules, int x, int y, int length, bool row)
    {
        var size = modules.GetLength(0);
        for (var i = 0; i < length; i++)
        {
            var xx = row ? x + i : x;
            var yy = row ? y : y + i;
            if (xx < 0 || yy < 0 || xx >= size || yy >= size)
                return false;
            if (modules[xx, yy])
                return false;
        }

        return true;
    }

    private sealed class QrMatrix
    {
        private readonly bool[,] _isFunction;

        public QrMatrix(int size)
        {
            Modules = new bool[size, size];
            _isFunction = new bool[size, size];
        }

        public bool[,] Modules { get; }
        private int Size => Modules.GetLength(0);

        public void DrawFunctionPatterns(int version)
        {
            DrawFinderPattern(3, 3);
            DrawFinderPattern(Size - 4, 3);
            DrawFinderPattern(3, Size - 4);

            var alignmentCenters = GetAlignmentPatternPositions(version);
            foreach (var x in alignmentCenters)
            {
                foreach (var y in alignmentCenters)
                {
                    if (_isFunction[x, y])
                        continue;

                    DrawAlignmentPattern(x, y);
                }
            }

            for (var i = 0; i < Size; i++)
            {
                SetFunctionModule(6, i, i % 2 == 0);
                SetFunctionModule(i, 6, i % 2 == 0);
            }

            DrawFormatBits(0);
            DrawVersionBits(version);
            SetFunctionModule(8, Size - 8, true);
        }

        public void DrawCodewords(byte[] codewords)
        {
            var bitIndex = 0;
            var upward = true;

            for (var right = Size - 1; right >= 1; right -= 2)
            {
                if (right == 6)
                    right--;

                for (var vert = 0; vert < Size; vert++)
                {
                    var y = upward ? Size - 1 - vert : vert;
                    for (var x = right; x >= right - 1; x--)
                    {
                        if (_isFunction[x, y])
                            continue;

                        var dark = false;
                        if (bitIndex < codewords.Length * 8)
                            dark = ((codewords[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;

                        Modules[x, y] = dark;
                        bitIndex++;
                    }
                }

                upward = !upward;
            }
        }

        public void ApplyMask(int mask)
        {
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (!_isFunction[x, y] && GetMaskBit(mask, x, y))
                        Modules[x, y] = !Modules[x, y];
                }
            }
        }

        public void DrawFormatBits(int mask)
        {
            var bits = GetFormatBits(mask);
            for (var i = 0; i <= 5; i++)
                SetFunctionModule(8, i, GetBit(bits, i));
            SetFunctionModule(8, 7, GetBit(bits, 6));
            SetFunctionModule(8, 8, GetBit(bits, 7));
            SetFunctionModule(7, 8, GetBit(bits, 8));
            for (var i = 9; i < 15; i++)
                SetFunctionModule(14 - i, 8, GetBit(bits, i));

            for (var i = 0; i < 8; i++)
                SetFunctionModule(Size - 1 - i, 8, GetBit(bits, i));
            for (var i = 8; i < 15; i++)
                SetFunctionModule(8, Size - 15 + i, GetBit(bits, i));
            SetFunctionModule(8, Size - 8, true);
        }

        public void DrawVersionBits(int version)
        {
            if (version < 7)
                return;

            var rem = version;
            for (var i = 0; i < 12; i++)
                rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
            var bits = (version << 12) | (rem & 0xFFF);

            for (var i = 0; i < 18; i++)
            {
                var bit = GetBit(bits, i);
                var a = Size - 11 + i % 3;
                var b = i / 3;
                SetFunctionModule(a, b, bit);
                SetFunctionModule(b, a, bit);
            }
        }

        private void DrawFinderPattern(int centerX, int centerY)
        {
            for (var dy = -4; dy <= 4; dy++)
            {
                for (var dx = -4; dx <= 4; dx++)
                {
                    var x = centerX + dx;
                    var y = centerY + dy;
                    if (x < 0 || y < 0 || x >= Size || y >= Size)
                        continue;

                    var distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    SetFunctionModule(x, y, distance != 2 && distance != 4);
                }
            }
        }

        private void DrawAlignmentPattern(int centerX, int centerY)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                for (var dx = -2; dx <= 2; dx++)
                    SetFunctionModule(centerX + dx, centerY + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
            }
        }

        private void SetFunctionModule(int x, int y, bool dark)
        {
            Modules[x, y] = dark;
            _isFunction[x, y] = true;
        }

        private static int[] GetAlignmentPatternPositions(int version)
            => version switch
            {
                1 => [],
                2 => [6, 18],
                3 => [6, 22],
                4 => [6, 26],
                5 => [6, 30],
                6 => [6, 34],
                7 => [6, 22, 38],
                8 => [6, 24, 42],
                9 => [6, 26, 46],
                10 => [6, 28, 50],
                _ => throw new ArgumentOutOfRangeException(nameof(version))
            };

        private static int GetFormatBits(int mask)
        {
            var data = (1 << 3) | mask;
            var rem = data;
            for (var i = 0; i < 10; i++)
                rem = (rem << 1) ^ ((rem >> 9) * 0x537);
            return ((data << 10) | (rem & 0x3FF)) ^ 0x5412;
        }

        private static bool GetMaskBit(int mask, int x, int y)
            => mask switch
            {
                0 => (x + y) % 2 == 0,
                1 => y % 2 == 0,
                2 => x % 3 == 0,
                3 => (x + y) % 3 == 0,
                4 => (y / 2 + x / 3) % 2 == 0,
                5 => x * y % 2 + x * y % 3 == 0,
                6 => (x * y % 2 + x * y % 3) % 2 == 0,
                7 => ((x + y) % 2 + x * y % 3) % 2 == 0,
                _ => throw new ArgumentOutOfRangeException(nameof(mask))
            };

        private static bool GetBit(int value, int index)
            => ((value >> index) & 1) != 0;
    }
}

internal static class ReedSolomon
{
    private static readonly byte[] Exp = new byte[512];
    private static readonly byte[] Log = new byte[256];

    static ReedSolomon()
    {
        var value = 1;
        for (var i = 0; i < 255; i++)
        {
            Exp[i] = (byte)value;
            Log[value] = (byte)i;
            value <<= 1;
            if (value >= 0x100)
                value ^= 0x11D;
        }

        for (var i = 255; i < Exp.Length; i++)
            Exp[i] = Exp[i - 255];
    }

    public static byte[] ComputeRemainder(byte[] data, int degree)
    {
        var generator = MakeGenerator(degree);
        var result = new byte[degree];

        foreach (var value in data)
        {
            var factor = (byte)(value ^ result[0]);
            Array.Copy(result, 1, result, 0, degree - 1);
            result[^1] = 0;

            for (var i = 0; i < degree; i++)
                result[i] ^= Multiply(generator[i + 1], factor);
        }

        return result;
    }

    private static byte[] MakeGenerator(int degree)
    {
        var result = new byte[] { 1 };
        for (var i = 0; i < degree; i++)
        {
            var next = new byte[result.Length + 1];
            for (var j = 0; j < result.Length; j++)
            {
                next[j] ^= result[j];
                next[j + 1] ^= Multiply(result[j], Exp[i]);
            }

            result = next;
        }

        return result;
    }

    private static byte Multiply(byte x, byte y)
    {
        if (x == 0 || y == 0)
            return 0;

        return Exp[Log[x] + Log[y]];
    }
}
