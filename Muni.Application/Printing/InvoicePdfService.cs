// InvoicePdfService.cs (encabezado)
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Globalization;
using Muni.Domain;

// Alias para evitar ambigüedad:
using QDoc = QuestPDF.Fluent.Document;
using QContainer = QuestPDF.Infrastructure.IContainer;
using QuestPDF.Helpers;


namespace Muni.Application.Printing;

public static class InvoicePdfService
{
    public static byte[] BuildPdf(Invoice inv, Vehicle v, Owner owner)
    {
        // Datos básicos
        var barcodePng = BarcodeImage.GeneratePng(inv.Barcode, useITF: true, width: 1000, height: 220, margin: 0);
        var importe = inv.Importe.ToString("N2", new CultureInfo("es-AR"));
        var vto1 = $"{inv.Vto1:dd/MM/yyyy}";
        var vto2 = $"{inv.Vto2:dd/MM/yyyy}";

        // Documento
        var doc = QDoc.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Municipalidad – Dirección de Rentas").SemiBold().FontSize(16);
                        col.Item().Text("Impuesto a los Vehículos – Comprobante de Pago (Pago Fácil)");
                        col.Item().Text($"Factura N° {inv.Numero}").Bold();
                    });
                    row.ConstantItem(140).Column(col =>
                    {
                        col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
                        col.Item().Text($"Moneda: ARS");
                    });
                });

                page.Content().Column(col =>
                {
                    // Datos del contribuyente / vehículo
                    col.Item().Text("Datos del Contribuyente").SemiBold().FontSize(13);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(3);
                        });
                        t.Cell().Element(CellLabel).Text("CUIT/CUIL:");
                        t.Cell().Element(CellValue).Text(owner.CuitCuil);
                        t.Cell().Element(CellLabel).Text("Contribuyente:");
                        t.Cell().Element(CellValue).Text(owner.Nombre);
                        t.Cell().Element(CellLabel).Text("Domicilio:");
                        t.Cell().Element(CellValue).Text(owner.Domicilio);
                    });

                    col.Item().PaddingTop(10).Text("Datos del Vehículo").SemiBold().FontSize(13);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1); c.RelativeColumn(1);
                            c.RelativeColumn(1); c.RelativeColumn(1);
                        });
                        t.Cell().Element(CellLabel).Text("Patente:");
                        t.Cell().Element(CellValue).Text(v.Patente);
                        t.Cell().Element(CellLabel).Text("Marca/Modelo:");
                        t.Cell().Element(CellValue).Text($"{v.Marca} {v.Modelo}");
                        t.Cell().Element(CellLabel).Text("Año:");
                        t.Cell().Element(CellValue).Text(v.Anio.ToString());
                        t.Cell().Element(CellLabel).Text("Categoría:");
                        t.Cell().Element(CellValue).Text(v.Categoria);
                    });

                    col.Item().PaddingTop(10).Text("Detalle de Liquidación").SemiBold().FontSize(13);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                        });
                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Periodo");
                            h.Cell().Element(HeaderCell).Text("Vencimiento 1");
                            h.Cell().Element(HeaderCell).Text("Vencimiento 2");
                        });
                        t.Cell().Element(CellValue).Text(inv.Obligation.Periodo);
                        t.Cell().Element(CellValue).Text(vto1);
                        t.Cell().Element(CellValue).Text(vto2);
                    });

                    col.Item().PaddingTop(6).AlignRight().Text($"Importe a pagar (1er vto): $ {importe}").Bold().FontSize(13);

                    // Código de barras
                    col.Item().PaddingVertical(14).Column(bar =>
                    {
                        bar.Item().Text("Código de Barras (Pago Fácil)").SemiBold().FontSize(13);
                        bar.Item().Image(barcodePng);
                        bar.Item().AlignCenter().PaddingTop(4).Text(inv.Barcode).FontSize(12);
                        bar.Item().AlignCenter().Text("(Imprima con buena calidad. No doble ni dañe el código)").FontColor("#666");
                    });

                    // Pie: info útil
                    col.Item().PaddingTop(8).Text(t =>
                    {
                        t.Span("Empresa PF (4): ").SemiBold();
                        t.Span(inv.EmpresaPF4);
                        t.Span("    Cliente (14): ").SemiBold();
                        t.Span(inv.Cliente14);
                        t.Span("    Moneda: ").SemiBold();
                        t.Span(inv.Moneda1);
                    });

                    col.Item().PaddingTop(10).Text("Instrucciones")
                        .SemiBold().FontSize(12);
                    col.Item().Text("- Puede pagar en Pago Fácil o vía botón de pago electrónico.")
                        .FontColor("#444");
                    col.Item().Text("- Conserve este comprobante para su registro.")
                        .FontColor("#444");
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Municipalidad – Dirección de Rentas · ").FontSize(9).FontColor("#666");
                    t.Span("Documento generado electrónicamente · ").FontSize(9).FontColor("#666");
                    t.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#666");
                });
             });
        });

        using var ms = new MemoryStream();
        doc.GeneratePdf(ms);
        return ms.ToArray();

        QContainer HeaderCell(QContainer c) => c.Padding(4).Background("#EEE").Border(0.5f).BorderColor("#CCC");
        QContainer CellLabel(QContainer c) => c.Padding(4).Background("#FAFAFA").Border(0.5f).BorderColor("#EEE").AlignLeft();
        QContainer CellValue(QContainer c) => c.Padding(4).Border(0.5f).BorderColor("#EEE").AlignLeft();

    }
}

