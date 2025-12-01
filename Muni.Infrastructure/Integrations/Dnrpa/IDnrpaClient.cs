using System.Threading;
using System.Threading.Tasks;

namespace Muni.Infrastructure.Integrations.Dnrpa;
public interface IDnrpaClient
{
    Task<IReadOnlyList<DnrpaTransaccionDto>> GetTransaccionesPorRangoAsync(DnrpaQuery query, CancellationToken ct);
    Task<IReadOnlyList<DnrpaTransaccionDto>> GetTransaccionesPorDniAsync(string dni, CancellationToken ct);
}

