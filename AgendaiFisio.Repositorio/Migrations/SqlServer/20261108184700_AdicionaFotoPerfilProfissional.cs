using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaiFisio.Repositorio.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AdicionaFotoPerfilProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoPerfilChave",
                table: "Profissionais",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoPerfilChave",
                table: "Profissionais");
        }
    }
}
