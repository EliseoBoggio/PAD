using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muni.Domain
{
    public class Payment
    {
        [Key] public Guid Id { get; set; }
        [Required] public Guid InvoiceId { get; set; }
        [Required, MaxLength(20)] public string Provider { get; set; } = "PAGOFACIL";
        public decimal Monto { get; set; }
        public DateTime FechaAcreditacion { get; set; }
        [MaxLength(60)] public string? ExternalId { get; set; }
        [Required, MaxLength(20)] public string Estado { get; set; } = "APPLIED";
    }
}
