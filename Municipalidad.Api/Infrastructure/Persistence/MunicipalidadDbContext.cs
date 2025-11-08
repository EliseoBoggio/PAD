using Microsoft.EntityFrameworkCore;
using Municipalidad.Api.Domain.Models;

namespace Municipalidad.Api.Infrastructure.Persistence;

public class MunicipalidadDbContext : DbContext
{
    public MunicipalidadDbContext(DbContextOptions<MunicipalidadDbContext> options) : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Titular> Titulares => Set<Titular>();
    public DbSet<TributoMensual> TributosMensuales => Set<TributoMensual>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<Pago> Pagos => Set<Pago>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(v => v.LicensePlate).IsUnique();
            entity.Property(v => v.LicensePlate).HasMaxLength(16);
            entity.HasOne(v => v.Owner)
                .WithMany(o => o.Vehicles)
                .HasForeignKey(v => v.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Titular>(entity =>
        {
            entity.HasIndex(t => t.DocumentNumber).IsUnique();
            entity.Property(t => t.DocumentNumber).HasMaxLength(32);
            entity.Property(t => t.Email).HasMaxLength(128);
        });

        modelBuilder.Entity<TributoMensual>(entity =>
        {
            entity.HasIndex(t => new { t.VehicleId, t.Period }).IsUnique();
            entity.Property(t => t.BaseAmount).HasColumnType("decimal(18,2)");
            entity.Property(t => t.Surcharges).HasColumnType("decimal(18,2)");
            entity.HasOne(t => t.Vehicle)
                .WithMany(v => v.Tributos)
                .HasForeignKey(t => t.VehicleId);
            entity.HasOne(t => t.Invoice)
                .WithMany(i => i.Tributos)
                .HasForeignKey(t => t.InvoiceId);
            entity.Property(t => t.Period)
                .HasConversion(
                    v => v.ToDateTime(TimeOnly.MinValue),
                    v => DateOnly.FromDateTime(v));
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.Property(f => f.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(f => f.InvoiceNumber).IsUnique();
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId);
        });
    }
}
