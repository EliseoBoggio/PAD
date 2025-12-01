using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muni.Domain
{
    public class TaxObligation
    {
        [Key] public Guid Id { get; set; }

        // Necesario para tu flujo (periodo y montos/vtos)
        [Required, MaxLength(6)] public string Periodo { get; set; } = default!; // "YYYYMM"
        public decimal ImportePrimerVenc { get; set; }
        public decimal RecargoSegundoVenc { get; set; }
        public DateOnly FechaPrimerVenc { get; set; }
        public DateOnly FechaSegundoVenc { get; set; }

        [Required] public Guid VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = default!;   // <-- navegación que te falta

        [Required, MaxLength(20)] public string Estado { get; set; } = "GENERADA"; // GENERADA/FACTURADA/PAGADA/VENCIDA
    }
}

