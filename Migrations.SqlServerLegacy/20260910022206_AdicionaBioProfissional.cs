using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaiFisio.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaBioProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Profissionais",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Profissionais");
        }
    }
}
