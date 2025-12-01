using Muni.Domain;
using Microsoft.EntityFrameworkCore;

namespace Muni.Infrastructure;


public static class BillingHelpers
{
    public static string MakeInvoiceNumber(MuniDbContext db)
    {
        var count = db.Invoices.LongCount() + 1;
        return $"001-{count.ToString().PadLeft(8, '0')}";
    }

    public static string BuildCliente14(TaxObligation obl, Owner owner)
    {
        var cuit11 = new string(owner.CuitCuil.Where(char.IsDigit).ToArray())
                        .PadLeft(11, '0')[..11];
        var yy = obl.Periodo.Substring(2, 2);
        var mm = obl.Periodo.Substring(4, 2);
        var baseStr = $"{cuit11}{yy}{mm}";
        return (baseStr + "0").PadLeft(14, '0')[..14];
    }
}


