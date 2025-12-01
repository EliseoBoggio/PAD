using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Muni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniquePaymentPerInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_ExternalId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_InvoiceId",
                table: "Payments",
                columns: new[] { "Provider", "InvoiceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_InvoiceId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_ExternalId",
                table: "Payments",
                columns: new[] { "Provider", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");
        }
    }
}
