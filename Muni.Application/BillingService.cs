// Muni.Application/BillingService.cs
using Muni.Domain;

namespace Muni.Application;

public interface IBillingService
{
    (decimal imp1, decimal recargo, DateOnly v1, DateOnly v2) Calcular(Vehicle v, string periodo);
}

public class BillingService : IBillingService
{
    public (decimal imp1, decimal recargo, DateOnly v1, DateOnly v2) Calcular(Vehicle v, string periodo)
    {
        // Regla base por categoría
        decimal baseImp = v.Categoria != null && v.Categoria.StartsWith("MOTO_>150")
            ? 12000m
            : 8500m;

        int year = int.Parse(periodo[..4]);
        int month = int.Parse(periodo[4..]);
        var v1 = new DateOnly(year, month, DateTime.DaysInMonth(year, month)); // último día
        var v2 = v1.AddDays(10);

        var imp2 = Math.Round(baseImp * 1.038m, 2); // 3.80% de recargo
        var recargo = Math.Round(imp2 - baseImp, 2);

        return (Math.Round(baseImp, 2), recargo, v1, v2);
    }
}

