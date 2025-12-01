using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muni.Domain
{
    public class Invoice
    {
        [Key] public Guid Id { get; set; }
        [Required] public Guid ObligationId { get; set; }
        public TaxObligation Obligation { get; set; } = default!;   // <-- navegación que te falta

        [Required, MaxLength(20)] public string Numero { get; set; } = default!;
        public decimal Importe { get; set; }
        public DateOnly Vto1 { get; set; }
        public DateOnly Vto2 { get; set; }

        [Required, MaxLength(42)] public string Barcode { get; set; } = default!;   // 42 recomendado
        [Required, MaxLength(4)] public string EmpresaPF4 { get; set; } = default!;
        [Required, MaxLength(14)] public string Cliente14 { get; set; } = default!;
        [Required, MaxLength(1)] public string Moneda1 { get; set; } = "0";
        [MaxLength(260)] public string? PdfPath { get; set; }
        [Required, MaxLength(20)] public string Estado { get; set; } = "EMITIDA";
    }

}
