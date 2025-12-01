using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Muni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDecimalsAndNavs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_Barcode",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "Invoices",
                type: "nvarchar(42)",
                maxLength: 42,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_VehicleId",
                table: "TaxObligations",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ObligationId",
                table: "Invoices",
                column: "ObligationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TaxObligations_ObligationId",
                table: "Invoices",
                column: "ObligationId",
                principalTable: "TaxObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxObligations_Vehicles_VehicleId",
                table: "TaxObligations",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TaxObligations_ObligationId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxObligations_Vehicles_VehicleId",
                table: "TaxObligations");

            migrationBuilder.DropIndex(
                name: "IX_TaxObligations_VehicleId",
                table: "TaxObligations");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ObligationId",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "Invoices",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(42)",
                oldMaxLength: 42);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Barcode",
                table: "Invoices",
                column: "Barcode",
                unique: true);
        }
    }
}
