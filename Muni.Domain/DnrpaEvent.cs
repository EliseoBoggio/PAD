namespace Muni.Domain;

public class DnrpaEvent
{
    public Guid Id { get; set; }
    public string ExternalKey { get; set; } = "";
    public string NumeroPatente { get; set; } = "";
    public string TipoTransaccion { get; set; } = "";
    public DateTime FechaTransaccion { get; set; }
    public DateTime AppliedAt { get; set; }
}

