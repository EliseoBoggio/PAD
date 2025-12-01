using Muni.Domain;
using System.ComponentModel.DataAnnotations;

public class Owner
{
    [Key] public Guid Id { get; set; }

    // valor por defecto si no tenemos CUIT real (luego lo podés sobreescribir)
    [Required, MaxLength(20)]
    public string CuitCuil { get; set; } = "SIN-DOC";

    [Required, MaxLength(150)] public string Nombre { get; set; } = default!;
    [MaxLength(200)] public string? Domicilio { get; set; }
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
