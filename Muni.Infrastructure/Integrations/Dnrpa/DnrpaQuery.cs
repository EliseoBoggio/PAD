namespace Muni.Infrastructure.Integrations.Dnrpa;

public class DnrpaQuery
{
    public DateTime Desde { get; set; }
    public DateTime? Hasta { get; set; }  // nullable porque en ellos también es opcional
}

