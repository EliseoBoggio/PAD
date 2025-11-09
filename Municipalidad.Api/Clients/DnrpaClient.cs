using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Municipalidad.Api.Domain.Models;

namespace Municipalidad.Api.Clients;

public class DnrpaOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class DnrpaClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DnrpaClient> _logger;
    private readonly DnrpaOptions _options;

    public DnrpaClient(HttpClient httpClient, IOptions<DnrpaOptions> options, ILogger<DnrpaClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<Vehicle?> GetVehicleAsync(string licensePlate, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/vehicles/{licensePlate}");
            request.Headers.Add("x-api-key", _options.ApiKey);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Vehicle>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vehicle {LicensePlate} from DNRPA", licensePlate);
            return null;
        }
    }
}
