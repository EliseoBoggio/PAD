using Microsoft.EntityFrameworkCore;
using Muni.Domain;
using Muni.Application; // IBillingService
using System.Security.Cryptography;
using System.Text;

namespace Muni.Infrastructure.Integrations.Dnrpa;

public class ImportResult
{
    public int EventosLeidos { get; set; }
    public int AltasAplicadas { get; set; }
    public int TransferenciasAplicadas { get; set; }
    public int OwnersCreados { get; set; }
    public int VehiculosCreados { get; set; }
    public int HistorialesCreados { get; set; }
    public int ObligacionesCreadas { get; set; }
    public List<string> Skipped { get; set; } = new();
}

public class DnrpaImporter : IDnrpaImporter
{
    private readonly IDnrpaClient _client;
    private readonly MuniDbContext _db;
    private readonly IBillingService _billing;

    public DnrpaImporter(IDnrpaClient client, MuniDbContext db, IBillingService billing)
    {
        _client = client; _db = db; _billing = billing;
    }

    // Importa por rango de fechas
    public async Task<ImportResult> ImportAsync(DnrpaQuery q, CancellationToken ct)
    {
        var eventos = await _client.GetTransaccionesPorRangoAsync(q, ct);
        return await ImportCoreAsync(eventos, null, ct);
    }

    // Importa por DNI
    public async Task<ImportResult> ImportByDniAsync(string dni, CancellationToken ct)
    {
        var eventos = await _client.GetTransaccionesPorDniAsync(dni, ct);
        return await ImportCoreAsync(eventos, dni, ct);
    }

    private async Task<ImportResult> ImportCoreAsync(
        IReadOnlyList<DnrpaTransaccionDto> eventos,
        string? dniContext,
        CancellationToken ct)
    {
        var res = new ImportResult
        {
            EventosLeidos = eventos.Count
        };

        // Para evitar duplicar eventos en el mismo batch
        var processedKeys = new HashSet<string>();

        // Cache en memoria para no chocar índices únicos antes de SaveChanges
        var ownersCache = new Dictionary<string, Owner>(StringComparer.OrdinalIgnoreCase);
        var vehiclesCache = new Dictionary<string, Vehicle>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in eventos.OrderBy(x => x.FechaTransaccion))
        {
            var key = MakeExternalKey(e);

            // 1) Duplicado dentro del MISMO batch
            if (processedKeys.Contains(key))
            {
                res.Skipped.Add(key);
                continue;
            }

            // 2) Ya existe en DB de corridas anteriores
            if (await _db.DnrpaEvents.AnyAsync(x => x.ExternalKey == key, ct))
            {
                res.Skipped.Add(key);
                continue;
            }

            processedKeys.Add(key);

            // -------- OWNER --------
            var nombre = (e.TitularDestino ?? "").Trim();
            Owner? ownerDest = null;

            if (!string.IsNullOrEmpty(nombre) && ownersCache.TryGetValue(nombre, out var cachedOwner))
            {
                ownerDest = cachedOwner;
            }
            else
            {
                ownerDest = await _db.Owners.FirstOrDefaultAsync(o => o.Nombre == nombre, ct);
                if (ownerDest != null && !string.IsNullOrEmpty(nombre))
                    ownersCache[nombre] = ownerDest;
            }

            if (ownerDest == null)
            {
                var cuit = dniContext;

                if (string.IsNullOrWhiteSpace(cuit))
                {
                    // SIN DNI: generamos CUIT interno único pero estable por nombre
                    var baseStr = nombre.Length == 0 ? "SIN-NOMBRE" : nombre;
                    using var sha = SHA256.Create();
                    var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(baseStr));
                    // 10 chars hex + prefijo = 11, entra en MaxLength(20)
                    var hashStr = Convert.ToHexString(hashBytes)[..10];
                    cuit = "X" + hashStr; // p.ej. X6199D336E5
                }

                ownerDest = new Owner
                {
                    Id = Guid.NewGuid(),
                    Nombre = nombre,
                    CuitCuil = cuit
                };

                _db.Owners.Add(ownerDest);
                if (!string.IsNullOrEmpty(nombre))
                    ownersCache[nombre] = ownerDest;
                res.OwnersCreados++;
            }
            else if (dniContext != null && (string.IsNullOrWhiteSpace(ownerDest.CuitCuil) || ownerDest.CuitCuil == "SIN-DOC"))
            {
                // Si antes lo habíamos creado sin doc y ahora tengo DNI, lo actualizo
                ownerDest.CuitCuil = dniContext;
            }

            // -------- VEHICLE --------
            var pat = (e.NumeroPatente ?? "").Trim().ToUpperInvariant();
            Vehicle? veh = null;

            if (!string.IsNullOrEmpty(pat) && vehiclesCache.TryGetValue(pat, out var cachedVeh))
            {
                veh = cachedVeh;
            }
            else
            {
                veh = await _db.Vehicles.FirstOrDefaultAsync(v => v.Patente == pat, ct);
                if (veh != null && !string.IsNullOrEmpty(pat))
                    vehiclesCache[pat] = veh;
            }

            if (veh == null)
            {
                veh = new Vehicle
                {
                    Id = Guid.NewGuid(),
                    Patente = pat,
                    Activo = true,
                    OwnerId = ownerDest.Id
                };
                _db.Vehicles.Add(veh);
                if (!string.IsNullOrEmpty(pat))
                    vehiclesCache[pat] = veh;
                res.VehiculosCreados++;
            }

            // Actualizo datos del vehículo siempre con la info más reciente
            veh.Marca = e.Marca;
            veh.Modelo = e.Modelo;
            veh.Anio = e.AnioFabricacion;
            veh.Categoria = e.CategoriaVehiculo;

            var tipo = e.TipoTransaccion?.Trim().ToUpperInvariant();
            var fechaDesde = DateOnly.FromDateTime(e.FechaTransaccion);

            if (tipo == "ALTA")
            {
                veh.OwnerId = ownerDest.Id;

                _db.OwnershipHistories.Add(new OwnershipHistory
                {
                    Id = Guid.NewGuid(),
                    VehicleId = veh.Id,
                    OwnerId = ownerDest.Id,
                    FechaDesde = fechaDesde,
                    Motivo = "ALTA"
                });
                res.HistorialesCreados++;

                var periodo = $"{e.FechaTransaccion:yyyyMM}";
                var existsObl = await _db.TaxObligations
                    .AnyAsync(o => o.VehicleId == veh.Id && o.Periodo == periodo, ct);

                if (!existsObl)
                {
                    var (imp1, rec, v1, v2) = _billing.Calcular(veh, periodo);
                    _db.TaxObligations.Add(new TaxObligation
                    {
                        Id = Guid.NewGuid(),
                        VehicleId = veh.Id,
                        Periodo = periodo,
                        ImportePrimerVenc = imp1,
                        RecargoSegundoVenc = rec,
                        FechaPrimerVenc = v1,
                        FechaSegundoVenc = v2,
                        Estado = "ABIERTA"
                    });
                    res.ObligacionesCreadas++;
                }

                res.AltasAplicadas++;
            }
            else if (tipo == "TRANSFERENCIA")
            {
                if (veh.OwnerId != ownerDest.Id)
                {
                    veh.OwnerId = ownerDest.Id;

                    _db.OwnershipHistories.Add(new OwnershipHistory
                    {
                        Id = Guid.NewGuid(),
                        VehicleId = veh.Id,
                        OwnerId = ownerDest.Id,
                        FechaDesde = fechaDesde,
                        Motivo = "TRANSFERENCIA"
                    });
                    res.HistorialesCreados++;
                }
                res.TransferenciasAplicadas++;
            }

            // -------- DnrpaEvent --------
            _db.DnrpaEvents.Add(new DnrpaEvent
            {
                Id = Guid.NewGuid(),
                ExternalKey = key,
                NumeroPatente = pat,
                TipoTransaccion = tipo ?? "",
                FechaTransaccion = e.FechaTransaccion.ToUniversalTime(),
                AppliedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        return res;
    }

    private static string MakeExternalKey(DnrpaTransaccionDto e)
    {
        var raw =
            $"{e.FechaTransaccion.ToUniversalTime():O}|{e.NumeroPatente?.Trim().ToUpper()}|{e.TipoTransaccion?.Trim().ToUpper()}|{e.EjemplarPatente?.Trim().ToUpper()}";
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        return hash;
    }
}


