namespace Municipalidad.Api.Domain.Models;

public class Pago
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Factura? Invoice { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string Status { get; set; } = "Pending";
}
