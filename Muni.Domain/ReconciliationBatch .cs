using System.ComponentModel.DataAnnotations;

namespace Muni.Domain
{
    public class ReconciliationBatch
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(20)] public string Provider { get; set; } = "PAGOFACIL";
        public DateOnly Fecha { get; set; }
        [MaxLength(120)] public string FileName { get; set; } = default!;
        [MaxLength(260)] public string? RawPath { get; set; }
        public int TxCount { get; set; }
        public decimal Total { get; set; }
    }
}
