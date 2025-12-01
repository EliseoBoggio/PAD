using Muni.Infrastructure.Integrations.Dnrpa;

public interface IDnrpaImporter
{
    Task<ImportResult> ImportAsync(DnrpaQuery q, CancellationToken ct);      // por rango
    Task<ImportResult> ImportByDniAsync(string dni, CancellationToken ct);   // por DNI
}

