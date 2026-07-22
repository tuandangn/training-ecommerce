using System.Text;

namespace NamEcommerce.Web.Framework.Services.QrCode;

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
