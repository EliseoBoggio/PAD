using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muni.Infrastructure.Integrations.PagoFacil;

public static class PagoFacilBarcode
{
    // AA+DDD (años 00..99, día juliano 001..366)
    public static string ToAADDD(DateOnly date)
    {
        int aa = date.Year % 100;
        int ddd = date.DayOfYear;
        return $"{aa:00}{ddd:000}";
    }

    private static string PadZeros(decimal amount, int intDigits, int decDigits)
    {
        var scaled = (long)Math.Round(amount * (decimal)Math.Pow(10, decDigits));
        return scaled.ToString().PadLeft(intDigits + decDigits, '0');
    }

    // DV según doc: secuencia (1,3,5,7,9,3,5,7,9,...) → sum(prod) → /2 → parte entera %10
    private static int DV(string numeric)
    {
        int[] seq = { 1, 3, 5, 7, 9, 3, 5, 7, 9 };
        long sum = 0;
        for (int i = 0; i < numeric.Length; i++)
        {
            int d = numeric[i] - '0';
            int w = seq[i % seq.Length];
            sum += d * w;
        }
        long half = sum / 2;
        return (int)(half % 10);
    }

    public static string Build(
        string empresa4,
        decimal importeV1,
        DateOnly vto1,
        string cliente14,
        decimal recargoV2,
        DateOnly vto2)
    {
        string importe8 = PadZeros(importeV1, 6, 2);
        string fecha1_5 = ToAADDD(vto1);
        string cliente14Z = cliente14.PadLeft(14, '0').Substring(Math.Max(0, cliente14.Length - 14));
        string moneda1 = "0";
        string recargo6 = PadZeros(recargoV2, 4, 2);
        var diffDays = (vto2.ToDateTime(TimeOnly.MinValue) - vto1.ToDateTime(TimeOnly.MinValue)).Days;
        if (diffDays < 0 || diffDays > 99) throw new ArgumentException("Dif. de días inválida para segundo venc.");
        string fecha2_2 = diffDays.ToString("00");

        string baseStr = empresa4 + importe8 + fecha1_5 + cliente14Z + moneda1 + recargo6 + fecha2_2;
        int dv1 = DV(baseStr);
        int dv2 = DV(baseStr + dv1.ToString());
        return baseStr + dv1 + dv2; // longitud total 42
    }
}


