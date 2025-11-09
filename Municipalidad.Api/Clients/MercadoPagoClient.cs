using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Municipalidad.Api.Clients;

public interface IMercadoPagoClient
{
    Task<string?> CreatePaymentButtonAsync(string invoiceNumber, decimal amount, CancellationToken cancellationToken);
    bool ValidateWebhookSignature(string signature);
}

public class MercadoPagoOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}

public class MercadoPagoClient : IMercadoPagoClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MercadoPagoClient> _logger;
    private readonly MercadoPagoOptions _options;

    public MercadoPagoClient(HttpClient httpClient, IOptions<MercadoPagoOptions> options, ILogger<MercadoPagoClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string?> CreatePaymentButtonAsync(string invoiceNumber, decimal amount, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/checkout/preferences");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);
            request.Content = JsonContent.Create(new
            {
                items = new[]
                {
                    new { title = $"Factura {invoiceNumber}", quantity = 1, unit_price = amount }
                }
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<MercadoPagoPreferenceResponse>(cancellationToken: cancellationToken);
            return payload?.InitPoint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating MercadoPago button for invoice {InvoiceNumber}", invoiceNumber);
            return null;
        }
    }

    public bool ValidateWebhookSignature(string signature)
    {
        // Simplified validation for demo purposes.
        return string.Equals(signature, _options.WebhookSecret, StringComparison.Ordinal);
    }

    private sealed record MercadoPagoPreferenceResponse(string Id, string InitPoint);
}
