using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Muni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDnrpaEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaDesde",
                table: "OwnershipHistories",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "DnrpaEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NumeroPatente = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoTransaccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaTransaccion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DnrpaEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DnrpaEvents_ExternalKey",
                table: "DnrpaEvents",
                column: "ExternalKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DnrpaEvents");

            migrationBuilder.DropColumn(
                name: "FechaDesde",
                table: "OwnershipHistories");
        }
    }
}
