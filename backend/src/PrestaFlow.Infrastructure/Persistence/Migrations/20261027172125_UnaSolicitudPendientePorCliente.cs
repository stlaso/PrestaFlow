using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrestaFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnaSolicitudPendientePorCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Solicitudes_ClienteId_Pendiente",
                table: "Solicitudes",
                column: "ClienteId",
                unique: true,
                filter: "[Estado] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Solicitudes_ClienteId_Pendiente",
                table: "Solicitudes");
        }
    }
}
