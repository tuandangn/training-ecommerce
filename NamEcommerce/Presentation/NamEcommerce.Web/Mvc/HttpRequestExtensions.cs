namespace NamEcommerce.Web.Mvc;

public static class HttpRequestExtensions
{
    /// <summary>
    /// Kiểm tra xem HttpRequest có phải là AJAX request (XMLHttpRequest, Fetch API, JSON, v.v.) hay không.
    /// </summary>
    public static bool IsAjaxRequest(this HttpRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // 1. Kiểm tra header X-Requested-With (Thường dùng bởi jQuery, Axios, Prototype)
        if (string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return true;

        // 2. Kiểm tra header Accept (Trình duyệt/Fetch gửi yêu cầu phản hồi JSON hoặc API)
        string acceptHeader = request.Headers["Accept"].ToString();
        if (!string.IsNullOrEmpty(acceptHeader) &&
            (acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
             acceptHeader.Contains("text/json", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 3. Kiểm tra Content-Type của dữ liệu gửi lên (Dùng khi Client POST/PUT dữ liệu JSON)
        string contentType = request.ContentType ?? string.Empty;
        if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("text/json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 4. Kiểm tra Sec-Fetch-Mode (Fetch API tự động thêm header này trong các trình duyệt hiện đại)
        // Khi gọi fetch(), giá trị thường là "cors" hoặc "same-origin", khác với điều hướng trang thông thường là "navigate"
        string secFetchMode = request.Headers["Sec-Fetch-Mode"].ToString();
        if (string.Equals(secFetchMode, "cors", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(secFetchMode, "same-origin", StringComparison.OrdinalIgnoreCase))
        {
            // Loại trừ trường hợp điều hướng trang thông thường (Fetch destination không phải là document)
            string secFetchDest = request.Headers["Sec-Fetch-Dest"].ToString();
            if (!string.Equals(secFetchDest, "document", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
