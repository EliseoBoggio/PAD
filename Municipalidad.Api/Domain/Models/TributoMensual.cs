namespace Municipalidad.Api.Domain.Models;

public class TributoMensual
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public DateOnly Period { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal Surcharges { get; set; }
    public decimal TotalAmount => BaseAmount + Surcharges;
    public Guid? InvoiceId { get; set; }
    public Factura? Invoice { get; set; }
}
