using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaiFisio.Repositorio.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AdicionaAceiteLgpdUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceiteLgpd",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AceiteLgpdEm",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AceiteLgpd",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AceiteLgpdEm",
                table: "Usuarios");
        }
    }
}
