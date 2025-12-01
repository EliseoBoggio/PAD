using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muni.Domain
{
    public class Vehicle
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(10)] public string Patente { get; set; } = default!;
        [MaxLength(50)] public string? Marca { get; set; }
        [MaxLength(80)] public string? Modelo { get; set; }
        public int Anio { get; set; }
        [Required, MaxLength(30)] public string Categoria { get; set; } = "MOTO_<=150";
        [Required] public Guid OwnerId { get; set; }
        public Owner Owner { get; set; } = default!;
        public bool Activo { get; set; } = true;
    }
}
