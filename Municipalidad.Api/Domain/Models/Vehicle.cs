namespace Municipalidad.Api.Domain.Models;

public class Vehicle
{
    public Guid Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public Guid OwnerId { get; set; }
    public Titular? Owner { get; set; }
    public ICollection<TributoMensual> Tributos { get; set; } = new List<TributoMensual>();
}
