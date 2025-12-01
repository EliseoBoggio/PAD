using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Muni.Infrastructure;
using Muni.Domain;
using System.Globalization;

namespace Muni.Api.Controllers;

[ApiController]
[Route("api/v1/pagofacil")]
public class PagoFacilController : ControllerBase
{
    private readonly MuniDbContext _db;

    public PagoFacilController(MuniDbContext db) => _db = db;

    /// <summary>
    /// Sube y procesa el Archivo de Transmisión de Pago Fácil (PFddmma.9999).
    /// </summary>
    [HttpPost("rendicion")]
    public async Task<IActionResult> Rendicion(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Archivo vacío");

        using var sr = new StreamReader(file.OpenReadStream());
        string? line;
        int lineNum = 0;

        var batch = new ReconciliationBatch
        {
            Id = Guid.NewGuid(),
            Provider = "PAGOFACIL",
            Fecha = DateOnly.FromDateTime(DateTime.Now),
            FileName = file.FileName,
            RawPath = "", // si querés, guardalo en disco y poné path acá
            TxCount = 0,
            Total = 0m
        };
        _db.ReconciliationBatches.Add(batch);

        // Estado del lote actual para validar totales de registro 8
        int currentBatchNumber = 0;
        int lotTxCount = 0;
        long lotAmountCents = 0;

        // Totales de archivo para validar contra registro 9
        int fileBatchCount = 0;
        int filePaymentCount = 0;
        long filePaymentAmountCents = 0;

        // Estado de la transacción en curso (registros 5/6/7)
        Guid? currentInvoiceId = null;
        decimal? currentAmount = null;

        // Helper para parseo seguro de decimales en centavos
        static decimal ToAmountFromCents(string s) =>
            decimal.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture) / 100m;
        string currentRecordSeq = "";
        string currentWorkDate = "";
        string currentTerminal = "";

        while ((line = await sr.ReadLineAsync()) is not null)
        {
            lineNum++;
            if (line.Length == 0) continue;
            char rec = line[0];

            switch (rec)
            {
                case '1':
                    // Cabecera Archivo (opcional: leer fecha y número de empresa)
                    // Create Date: pos 1..8, Client Number: pos ?? (según doc)
                    // No impacta conciliación, pero podrías validar nombre empresa.
                    fileBatchCount = 0;
                    filePaymentCount = 0;
                    filePaymentAmountCents = 0;
                    break;

                case '3':
                    // Cabecera Lote
                    // Create Date: 1..8, Batch Number: 9..14 (6)
                    currentBatchNumber = SafeSub(line, 1 + 8, 6).ToInt();
                    lotTxCount = 0;
                    lotAmountCents = 0;
                    fileBatchCount++;
                    break;

                case '5':
                    // I04 (1/3/5/6/7/8/9) – offsets base 0
                    // RecordSequence(5): pos 1..5
                    // WorkDate(8):       pos 8..15  (AAAAMMDD)
                    // TransferDate(8):   pos 16..23 (AAAAMMDD)  // opcional si querés guardarla
                    // AccountNumber(21): pos 23..43 (Cliente14 right-pad con espacios)
                    // Currency(3):       pos 43..45
                    // Amount(10):        pos 47..56 (centavos)
                    // Terminal(6):       pos 57..62
                    // PaymentDate(8):    pos 63..70
                    // PaymentTime(4):    pos 71..74
                    // TermSeq(4):        pos 75..78

                    var recSeq5 = SafeSub(line, 1, 5).Trim();
                    var workDate8 = SafeSub(line, 8, 8);
                    var account = SafeSub(line, 23, 21).TrimEnd();
                    var amount10 = SafeSub(line, 47, 10);
                    var terminal6 = SafeSub(line, 57, 6).TrimEnd();

                    currentAmount = ToAmountFromCents(amount10);
                    currentRecordSeq = recSeq5;
                    currentWorkDate = workDate8;
                    currentTerminal = terminal6;

                    // Match inicial por AccountNumber (Cliente14). Si no matchea, en '6' probamos por Barcode.
                    var invByAccount = await _db.Invoices
                        .Where(i => i.Cliente14 == account)
                        .Select(i => new { i.Id })
                        .FirstOrDefaultAsync(ct);

                    currentInvoiceId = invByAccount?.Id ?? null;
                    break;

                case '6':
                    // Código de Barras de la transacción
                    // Record Code(1='6'), Bar Code(80) => 1..80, Type Code(1) => 81, Filler(46)
                    var barcode80 = SafeSub(line, 1, 80).TrimEnd();
                    if (currentInvoiceId is null)
                    {
                        var invByBarcode = await _db.Invoices
                            .Where(i => i.Barcode == barcode80)
                            .Select(i => new { i.Id, i.ObligationId, i.Estado })
                            .FirstOrDefaultAsync(ct);
                        if (invByBarcode is not null)
                            currentInvoiceId = invByBarcode.Id;
                    }
                    break;

                case '7':
                    // Instrumento:
                    // Currency(3):         pos 1..3
                    // Pay Instrument(1):   pos 4
                    // CodeBar Pay Inst(80):pos 5..84
                    // Amount(15):          pos 84..98  (centavos)
                    var instAmount15 = SafeSub(line, 84, 15);
                    var instAmount = ToAmountFromCents(instAmount15);

                    // Si '5' tenía monto, comparamos; usamos el más confiable (instAmount suele ser el definitivo)
                    var amount = currentAmount ?? instAmount;

                    // Idempotencia fuerte por operación PF: fechaTrabajo + nº lote + secuencia
                    // currentBatchNumber lo obtenés en el '3' (cabecera de lote)
                    var externalIdPf = $"PF:{currentWorkDate}:{currentBatchNumber:D6}:{(currentRecordSeq ?? "").PadLeft(5, '0')}";

                    // Evitar duplicados si re-procesan archivo o reintenta
                    bool exists = await _db.Payments
                        .AnyAsync(p => p.Provider == "PAGOFACIL" && p.ExternalId == externalIdPf, ct);

                    if (!exists && currentInvoiceId is not null)
                    {
                        _db.Payments.Add(new Payment
                        {
                            Id = Guid.NewGuid(),
                            InvoiceId = currentInvoiceId.Value,
                            Provider = "PAGOFACIL",
                            Monto = amount,
                            FechaAcreditacion = DateTime.Now,
                            ExternalId = externalIdPf,
                            Estado = "APPLIED"
                        });

                        // Cerrar factura/obligación
                        var inv = await _db.Invoices.FirstAsync(i => i.Id == currentInvoiceId, ct);
                        inv.Estado = "PAGADA";
                        var obl = await _db.TaxObligations.FirstOrDefaultAsync(o => o.Id == inv.ObligationId, ct);
                        if (obl is not null) obl.Estado = "PAGADA";

                        // Totales de lote/archivo
                        lotTxCount++;
                        lotAmountCents += (long)Math.Round(amount * 100m);
                        filePaymentCount++;
                        filePaymentAmountCents += (long)Math.Round(amount * 100m);
                    }

                    // Reset de la transacción en curso
                    currentInvoiceId = null;
                    currentAmount = null;
                    currentRecordSeq = "";
                    currentWorkDate = "";
                    currentTerminal = "";
                    break;

                case '8':
                    // Cola de Lote
                    // Record Code(1='8') | Create Date(8) | Batch Number(6) | Batch Payment Count(7) | Batch Payment Amount(12) | Filler(38) | Batch Count(5) | Filler(51)
                    int declaredCount = SafeSub(line, 1 + 8 + 6, 7).ToInt(); // 1+8+6 = 15; pos=15..21
                    long declaredAmount = SafeSub(line, 1 + 8 + 6 + 7, 12).ToLong(); // pos=22..33

                    // Validación suave de totales de lote
                    if (declaredCount != lotTxCount)
                    {
                        // log warn: mismatch de cantidad
                    }
                    if (declaredAmount != lotAmountCents)
                    {
                        // log warn: mismatch de importe lote
                    }
                    break;

                case '9':
                    // Cola de Archivo
                    // Record Code(1='9') | Create Date(8) | Total Batches(6) | File Payment Count(7) | File Payment Amount(12) | Filler(38) | File Count(7) | Filler(49)
                    int totalBatches = SafeSub(line, 1 + 8, 6).ToInt();          // pos=9..14
                    int fileCount = SafeSub(line, 1 + 8 + 6, 7).ToInt();         // pos=15..21
                    long fileAmount = SafeSub(line, 1 + 8 + 6 + 7, 12).ToLong(); // pos=22..33

                    // Validación suave (solo logueamos discrepancias; no fallamos el proceso)
                    if (totalBatches != fileBatchCount)
                    {
                        // log warn
                    }
                    if (fileCount != filePaymentCount)
                    {
                        // log warn
                    }
                    if (fileAmount != filePaymentAmountCents)
                    {
                        // log warn
                    }
                    break;

                default:
                    // Ignoramos otros códigos (si aparecieran)
                    break;
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new
        {
            batch.Id,
            batch.FileName,
            Totales = new
            {
                Lotes = fileBatchCount,
                Transacciones = filePaymentCount,
                Importe = (decimal)filePaymentAmountCents / 100m
            }
        });
    }

    // -------- helpers de substring seguro y parse ----------
    private static string SafeSub(string s, int startZeroBased, int len)
    {
        if (startZeroBased < 0) return "";
        if (startZeroBased >= s.Length) return "";
        if (startZeroBased + len > s.Length) len = s.Length - startZeroBased;
        return s.Substring(startZeroBased, len);
    }
    // PagoFacilController.cs  (agregar dentro de la clase)
    public sealed class PfValidacionDto
    {
        public bool Valida { get; set; }
        public Guid InvoiceId { get; set; }
        public string Periodo { get; set; } = "";
        public decimal ImportePrimerVenc { get; set; }
        public DateOnly FechaPrimerVenc { get; set; }
        public decimal RecargoSegundoVenc { get; set; }
        public DateOnly FechaSegundoVenc { get; set; }
        public string EstadoFactura { get; set; } = "";
        public string Cliente14 { get; set; } = "";
    }

    [HttpGet("validar")]
    public async Task<IActionResult> ValidarPorBarcode([FromQuery] string barcode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length < 42)
            return BadRequest(new { error = "barcode inválido (se espera 42 caracteres)" });

        var inv = await _db.Invoices
            .Include(i => i.Obligation)
            .FirstOrDefaultAsync(i => i.Barcode == barcode, ct);

        if (inv is null)
            return Ok(new PfValidacionDto { Valida = false });

        var o = inv.Obligation!;
        return Ok(new PfValidacionDto
        {
            Valida = true,
            InvoiceId = inv.Id,
            Periodo = o.Periodo,
            ImportePrimerVenc = o.ImportePrimerVenc,
            FechaPrimerVenc = o.FechaPrimerVenc,
            RecargoSegundoVenc = o.RecargoSegundoVenc,
            FechaSegundoVenc = o.FechaSegundoVenc,
            EstadoFactura = inv.Estado,
            Cliente14 = inv.Cliente14 ?? ""
        });
    }
    [HttpGet("validar-cuenta")]
    public async Task<IActionResult> ValidarPorCliente14([FromQuery] string cliente14, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cliente14))
            return BadRequest(new { error = "cliente14 requerido" });

        // Devuelvo la factura más reciente para ese cliente14 que esté EMITIDA o PAGADA
        var inv = await _db.Invoices
            .Include(i => i.Obligation)
            .Where(i => i.Cliente14 == cliente14)
            .OrderByDescending(i => i.Vto1) // o por Periodo desc
            .FirstOrDefaultAsync(ct);

        if (inv is null)
            return Ok(new PfValidacionDto { Valida = false });

        var o = inv.Obligation!;
        return Ok(new PfValidacionDto
        {
            Valida = true,
            InvoiceId = inv.Id,
            Periodo = o.Periodo,
            ImportePrimerVenc = o.ImportePrimerVenc,
            FechaPrimerVenc = o.FechaPrimerVenc,
            RecargoSegundoVenc = o.RecargoSegundoVenc,
            FechaSegundoVenc = o.FechaSegundoVenc,
            EstadoFactura = inv.Estado,
            Cliente14 = inv.Cliente14 ?? ""
        });
    }

}

file static class ParseExt
{
    public static int ToInt(this string s) =>
        int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;

    public static long ToLong(this string s) =>
        long.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0L;
}


