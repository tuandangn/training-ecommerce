using System.Text.RegularExpressions;

namespace NamEcommerce.Domain.Shared.Helpers;

public static class TextHelper
{
    private static readonly string[,] _replacedChars = new string[,]{
            { "ä|à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ|Ä|À|Á|Ạ|Ả|Ã|Â|Ầ|Ấ|Ậ|Ẩ|Ẫ|Ă|Ằ|Ắ|Ặ|Ẳ|Ẵ", "a"},
            { "ç|Ç", "c" },
            { "è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ|È|É|Ẹ|Ẻ|Ẽ|Ê|Ề|Ế|Ệ|Ể|Ễ","e"},
            { "ì|í|î|ị|ỉ|ĩ|Ì|Í|Ị|Ỉ|Ĩ", "i"},
            { "ö|ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ|Ö|Ò|Ó|Ọ|Ỏ|Õ|Ô|Ồ|Ố|Ộ|Ổ|Ỗ|Ơ|Ờ|Ớ|Ợ|Ở|Ỡ", "o"},
            { "ü|ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ|Ü|Ù|Ú|Ụ|Ủ|Ũ|Ư|Ừ|Ứ|Ự|Ử|Ữ", "u"},
            { "ỳ|ý|ỵ|ỷ|ỹ|Ỳ|Ý|Ỵ|Ỷ|Ỹ", "y" },
            { "đ|Đ", "d" }
        };

    public static string Normalize(string? content, bool toLowercase = false, bool replaceWhiteSpace = false)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        if (replaceWhiteSpace)
            content = content.Replace(" ", "");
        else
            content = content.Trim();
        var asciiStr = ReplaceCharacter(content, _replacedChars);

        return toLowercase ? asciiStr.ToLowerInvariant() : asciiStr.ToUpperInvariant();
    }

    private static string ReplaceCharacter(string target, string[,] charList)
    {
        for (var i = 0; i <= charList.GetUpperBound(0); i++)
        {
            target = Regex.Replace(target, charList[i, 0], charList[i, 1]);
        }
        return target;
    }
}
