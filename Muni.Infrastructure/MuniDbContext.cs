using Microsoft.EntityFrameworkCore;
using Muni.Domain;

namespace Muni.Infrastructure;

public class MuniDbContext : DbContext
{
    public MuniDbContext(DbContextOptions<MuniDbContext> options) : base(options) { }

    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<OwnershipHistory> OwnershipHistories => Set<OwnershipHistory>();
    public DbSet<TaxObligation> TaxObligations => Set<TaxObligation>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ReconciliationBatch> ReconciliationBatches => Set<ReconciliationBatch>();
    public DbSet<DnrpaEvent> DnrpaEvents => Set<DnrpaEvent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // -------- Claves primarias --------
        b.Entity<Owner>().HasKey(x => x.Id);
        b.Entity<Vehicle>().HasKey(x => x.Id);
        b.Entity<OwnershipHistory>().HasKey(x => x.Id);
        b.Entity<TaxObligation>().HasKey(x => x.Id);
        b.Entity<Invoice>().HasKey(x => x.Id);
        b.Entity<Payment>().HasKey(x => x.Id);
        b.Entity<ReconciliationBatch>().HasKey(x => x.Id);

        // -------- Owner --------
        b.Entity<Owner>().HasIndex(x => x.CuitCuil).IsUnique();
        b.Entity<Owner>().Property(x => x.CuitCuil).HasMaxLength(20).IsRequired();
        b.Entity<Owner>().Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        b.Entity<Owner>().Property(x => x.Domicilio).HasMaxLength(200);

        // -------- Vehicle --------
        b.Entity<Vehicle>().HasIndex(x => x.Patente).IsUnique();
        b.Entity<Vehicle>().Property(x => x.Patente).HasMaxLength(10).IsRequired();
        b.Entity<Vehicle>().Property(x => x.Marca).HasMaxLength(50);
        b.Entity<Vehicle>().Property(x => x.Modelo).HasMaxLength(50);
        b.Entity<Vehicle>().Property(x => x.Categoria).HasMaxLength(30);

        b.Entity<Vehicle>()
            .HasOne(v => v.Owner)
            .WithMany(o => o.Vehicles) // usa la colección del Owner
            .HasForeignKey(v => v.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);


        // -------- OwnershipHistory --------
        b.Entity<OwnershipHistory>()
            .HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<OwnershipHistory>()
            .HasOne<Owner>().WithMany().HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // -------- TaxObligation --------
        b.Entity<TaxObligation>().Property(o => o.ImportePrimerVenc).HasColumnType("decimal(18,2)");
        b.Entity<TaxObligation>().Property(o => o.RecargoSegundoVenc).HasColumnType("decimal(18,2)");
        b.Entity<TaxObligation>().Property(x => x.Periodo).HasMaxLength(6).IsRequired();
        b.Entity<TaxObligation>().Property(x => x.Estado).HasMaxLength(20).IsRequired();

        // Unicidad: una obligación por (vehículo, período)
        b.Entity<TaxObligation>().HasIndex(x => new { x.VehicleId, x.Periodo }).IsUnique();

        b.Entity<TaxObligation>()
            .HasOne(o => o.Vehicle)
            .WithMany()
            .HasForeignKey(o => o.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Check de formato de período (YYYYMM)
        b.Entity<TaxObligation>()
         .ToTable(t =>
         {
             t.HasCheckConstraint("CK_TaxObligation_PeriodoFmt", "LEN([Periodo])=6 AND [Periodo] NOT LIKE '%[^0-9]%'");
         });

        // -------- Invoice --------
        b.Entity<Invoice>().Property(i => i.Importe).HasColumnType("decimal(18,2)");
        b.Entity<Invoice>().Property(i => i.Barcode).HasMaxLength(42).IsRequired();
        b.Entity<Invoice>().Property(i => i.Cliente14).HasMaxLength(21);
        b.Entity<Invoice>().Property(i => i.EmpresaPF4).HasMaxLength(4);
        b.Entity<Invoice>().Property(i => i.Moneda1).HasMaxLength(1);
        b.Entity<Invoice>().Property(i => i.Estado).HasMaxLength(20).IsRequired();

        // Unicidades: barcode y 1 factura por obligación (MVP)
        b.Entity<Invoice>().HasIndex(i => i.Barcode).IsUnique();
        b.Entity<Invoice>().HasIndex(i => i.ObligationId).IsUnique();

        b.Entity<Invoice>()
            .HasOne(i => i.Obligation)
            .WithMany()
            .HasForeignKey(i => i.ObligationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Checks básicos
        b.Entity<Invoice>()
         .ToTable(t =>
         {
             t.HasCheckConstraint("CK_Invoice_Importe_Pos", "[Importe] >= 0");
             t.HasCheckConstraint("CK_Invoice_Barcode_Len", "LEN([Barcode]) BETWEEN 30 AND 60"); // tu barra ~42
         });

        // -------- Payment --------
        b.Entity<Payment>().Property(p => p.Monto).HasColumnType("decimal(18,2)");
        b.Entity<Payment>().Property(p => p.Provider).HasMaxLength(20).IsRequired();  // "MP" / "PAGOFACIL"
        b.Entity<Payment>().Property(p => p.ExternalId).HasMaxLength(64);
        b.Entity<Payment>().Property(p => p.Estado).HasMaxLength(20).IsRequired();

        // Idempotencia de webhooks/rendiciones
        // OnModelCreating
        b.Entity<Payment>()
         .HasIndex(p => new { p.Provider, p.InvoiceId })
         .IsUnique();


        b.Entity<Payment>()
            .HasOne<Invoice>().WithMany().HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        // Si preferís bloquear borrado de facturas con pagos, cambiá a Restrict.

        // -------- ReconciliationBatch --------
        b.Entity<ReconciliationBatch>().Property(r => r.Provider).HasMaxLength(20).IsRequired();
        b.Entity<ReconciliationBatch>().Property(r => r.FileName).HasMaxLength(80);
        b.Entity<ReconciliationBatch>().Property(r => r.Total).HasColumnType("decimal(18,2)");

        // -------- Concurrency (opcional, recomendado) --------
        b.Entity<Invoice>().Property<byte[]>("RowVersion").IsRowVersion();
        b.Entity<Payment>().Property<byte[]>("RowVersion").IsRowVersion();
        b.Entity<TaxObligation>().Property<byte[]>("RowVersion").IsRowVersion();

        // .NET 8 + EF Core 8 con SQL Server mapea DateOnly correctamente.
        b.Entity<DnrpaEvent>().HasKey(x => x.Id);
        b.Entity<DnrpaEvent>()
        .HasIndex(x => x.ExternalKey)
        .IsUnique();
    }
}

