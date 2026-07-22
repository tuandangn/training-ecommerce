namespace NamEcommerce.Web.Framework.Services.QrCode;

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
