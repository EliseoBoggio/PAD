using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muni.Domain
{
    public class OwnershipHistory
    {
        [Key] public Guid Id { get; set; }
        [Required] public Guid VehicleId { get; set; }
        [Required] public Guid OwnerId { get; set; }
        public DateOnly Desde { get; set; }
        public DateOnly? Hasta { get; set; }
        [MaxLength(30)] public string Motivo { get; set; } = "ALTA";
        public DateOnly FechaDesde { get; set; }   // inicio de la titularidad
    }
}
