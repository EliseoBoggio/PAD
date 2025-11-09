using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Municipalidad.Api.Clients;

public interface IPagoFacilClient
{
    Task<string?> GenerateBarcodeAsync(string invoiceNumber, decimal amount, CancellationToken cancellationToken);
}

public class PagoFacilOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string CommerceId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class PagoFacilClient : IPagoFacilClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PagoFacilClient> _logger;
    private readonly PagoFacilOptions _options;

    public PagoFacilClient(HttpClient httpClient, IOptions<PagoFacilOptions> options, ILogger<PagoFacilClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string?> GenerateBarcodeAsync(string invoiceNumber, decimal amount, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_options.BaseUrl}/barcodes", new
            {
                commerceId = _options.CommerceId,
                invoiceNumber,
                amount
            }, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<PagoFacilBarcodeResponse>(cancellationToken: cancellationToken);
            return payload?.Barcode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Pago Fácil barcode for invoice {InvoiceNumber}", invoiceNumber);
            return null;
        }
    }

    private sealed record PagoFacilBarcodeResponse(string Barcode);
}
