namespace Municipalidad.Api.Domain.Models;

public class Factura
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedOn { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public ICollection<TributoMensual> Tributos { get; set; } = new List<TributoMensual>();
    public ICollection<Pago> Payments { get; set; } = new List<Pago>();
}
