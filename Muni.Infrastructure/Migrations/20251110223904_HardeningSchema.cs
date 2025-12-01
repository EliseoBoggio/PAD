using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Muni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardeningSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxObligations_VehicleId",
                table: "TaxObligations");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ObligationId",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "Modelo",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TaxObligations",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "ReconciliationBatches",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "Payments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60,
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Payments",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Owners",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Cliente14",
                table: "Invoices",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(14)",
                oldMaxLength: 14);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_VehicleId_Periodo",
                table: "TaxObligations",
                columns: new[] { "VehicleId", "Periodo" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaxObligation_PeriodoFmt",
                table: "TaxObligations",
                sql: "LEN([Periodo])=6 AND [Periodo] NOT LIKE '%[^0-9]%'");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Barcode",
                table: "Invoices",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ObligationId",
                table: "Invoices",
                column: "ObligationId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Invoice_Barcode_Len",
                table: "Invoices",
                sql: "LEN([Barcode]) BETWEEN 30 AND 60");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Invoice_Importe_Pos",
                table: "Invoices",
                sql: "[Importe] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaxObligations_VehicleId_Periodo",
                table: "TaxObligations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaxObligation_PeriodoFmt",
                table: "TaxObligations");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Barcode",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ObligationId",
                table: "Invoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Invoice_Barcode_Len",
                table: "Invoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Invoice_Importe_Pos",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TaxObligations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "Modelo",
                table: "Vehicles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "ReconciliationBatches",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "Payments",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Owners",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Cliente14",
                table: "Invoices",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21);

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_VehicleId",
                table: "TaxObligations",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ObligationId",
                table: "Invoices",
                column: "ObligationId");
        }
    }
}
