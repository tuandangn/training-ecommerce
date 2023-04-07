namespace NamEcommerce.Admin.Client.Models.Common;

public static class ResponseModel
{
    public static ResponseModel<TData> Success<TData>(TData data, string? message = null)
        => new ResponseModel<TData>
        {
            Success = true,
            Data = data
        };

    public static ResponseModel<TData> Failed<TData>(string message)
        => new ResponseModel<TData>
        {
            Success = false,
            Message = message
        };
}

[Serializable]
public sealed class ResponseModel<TData>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TData? Data { get; set; }
}
