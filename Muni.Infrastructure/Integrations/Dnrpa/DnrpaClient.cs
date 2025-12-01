using System.Net.Http.Json;

namespace Muni.Infrastructure.Integrations.Dnrpa;

public class DnrpaClient : IDnrpaClient
{
    private readonly HttpClient _http;

    public DnrpaClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<DnrpaTransaccionDto>> GetTransaccionesPorRangoAsync(
        DnrpaQuery query,
        CancellationToken ct)
    {
        var qs = $"?desde={Uri.EscapeDataString(query.Desde.ToString("O"))}";
        if (query.Hasta != null)
            qs += $"&hasta={Uri.EscapeDataString(query.Hasta.Value.ToString("O"))}";

        var resp = await _http.GetAsync($"api/transacciones/obtener-por-rango{qs}", ct);
        resp.EnsureSuccessStatusCode();

        var data = await resp.Content.ReadFromJsonAsync<List<DnrpaTransaccionDto>>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);

        return data ?? new List<DnrpaTransaccionDto>();
    }

    public async Task<IReadOnlyList<DnrpaTransaccionDto>> GetTransaccionesPorDniAsync(
        string dni,
        CancellationToken ct)
    {
        var qs = $"?dni={Uri.EscapeDataString(dni)}";

        var resp = await _http.GetAsync($"api/transacciones/obtener-por-dni{qs}", ct);
        resp.EnsureSuccessStatusCode();

        var data = await resp.Content.ReadFromJsonAsync<List<DnrpaTransaccionDto>>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);

        return data ?? new List<DnrpaTransaccionDto>();
    }
}


