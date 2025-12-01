using System.Text.Json.Serialization;

namespace Muni.Infrastructure.Integrations.Dnrpa;

public class DnrpaTransaccionDto
{
    [JsonPropertyName("fechaTransaccion")]
    public DateTime FechaTransaccion { get; set; }

    [JsonPropertyName("titularOrigen")]
    public string? TitularOrigen { get; set; }

    [JsonPropertyName("titularDestino")]
    public string TitularDestino { get; set; } = null!;

    [JsonPropertyName("costoOperacion")]
    public decimal CostoOperacion { get; set; }

    [JsonPropertyName("tipoTransaccion")]
    public string TipoTransaccion { get; set; } = null!; // "ALTA" | "TRANSFERENCIA"

    [JsonPropertyName("marca")]
    public string Marca { get; set; } = null!;

    [JsonPropertyName("modelo")]
    public string Modelo { get; set; } = null!;

    [JsonPropertyName("anioFabricacion")]
    public int AnioFabricacion { get; set; }

    [JsonPropertyName("numeroMotor")]
    public string NumeroMotor { get; set; } = null!;

    [JsonPropertyName("categoriaVehiculo")]
    public string CategoriaVehiculo { get; set; } = null!;

    [JsonPropertyName("numeroPatente")]
    public string NumeroPatente { get; set; } = null!;

    [JsonPropertyName("ejemplarPatente")]
    public string EjemplarPatente { get; set; } = null!;
}
