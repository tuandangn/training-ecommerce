using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Application.Services.Debts;

public sealed class CassoTransactionClient(
    HttpClient httpClient,
    BankTransferPaymentSettings settings) : ICassoTransactionClient
{
    public async Task<CassoTransactionPageAppDto> GetTransactionsAsync(
        GetCassoTransactionsAppDto dto,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(settings.Casso.ApiKey))
            throw new InvalidOperationException("Error.CassoApiKeyRequired");

        httpClient.BaseAddress = new Uri(settings.Casso.ApiBaseUrl.TrimEnd('/') + "/");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Apikey", settings.Casso.ApiKey);

        var url = $"v2/transactions?fromDate={dto.FromDate:yyyy-MM-dd}&toDate={dto.ToDate:yyyy-MM-dd}&page={dto.Page}&pageSize={dto.PageSize}&sort=ASC";
        var response = await httpClient.GetFromJsonAsync<CassoTransactionsApiResponse>(url, cancellationToken).ConfigureAwait(false);
        var records = response?.Data?.Records ?? [];

        return new CassoTransactionPageAppDto
        {
            Records = records,
            HasMore = records.Count >= dto.PageSize
        };
    }

    private sealed record CassoTransactionsApiResponse
    {
        [JsonPropertyName("data")]
        public CassoTransactionsApiData? Data { get; init; }
    }

    private sealed record CassoTransactionsApiData
    {
        [JsonPropertyName("records")]
        public IList<CassoTransactionAppDto> Records { get; init; } = [];
    }
}
