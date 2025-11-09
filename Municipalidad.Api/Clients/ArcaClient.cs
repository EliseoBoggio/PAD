using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Municipalidad.Api.Domain.Models;

namespace Municipalidad.Api.Clients;

public class ArcaOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class ArcaClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArcaClient> _logger;
    private readonly ArcaOptions _options;

    public ArcaClient(HttpClient httpClient, IOptions<ArcaOptions> options, ILogger<ArcaClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string?> AuthorizeInvoiceAsync(Factura factura, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/invoices");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.Token);
            request.Content = JsonContent.Create(new
            {
                factura.InvoiceNumber,
                factura.Amount,
                factura.IssuedOn
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<ArcaAuthorizationResponse>(cancellationToken: cancellationToken);
            return payload?.AuthorizationCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authorizing invoice {InvoiceNumber}", factura.InvoiceNumber);
            return null;
        }
    }

    private sealed record ArcaAuthorizationResponse(string AuthorizationCode);
}
