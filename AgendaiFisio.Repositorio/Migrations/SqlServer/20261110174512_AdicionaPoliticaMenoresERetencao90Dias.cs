using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaiFisio.Repositorio.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AdicionaPoliticaMenoresERetencao90Dias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConsentimentoResponsavelLegal",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentimentoResponsavelLegalEm",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DadosExpiramEmUtc",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeResponsavelLegal",
                table: "Usuarios",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersaoTermos",
                table: "Usuarios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Usuarios]
                SET [DadosExpiramEmUtc] = DATEADD(day, 90, SYSUTCDATETIME()),
                    [VersaoTermos] = 'legado-2026-11-10'
                WHERE [DadosExpiramEmUtc] IS NULL OR [VersaoTermos] IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DadosExpiramEmUtc",
                table: "Usuarios",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VersaoTermos",
                table: "Usuarios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_DadosExpiramEmUtc",
                table: "Usuarios",
                column: "DadosExpiramEmUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_DadosExpiramEmUtc",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ConsentimentoResponsavelLegal",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ConsentimentoResponsavelLegalEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DadosExpiramEmUtc",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NomeResponsavelLegal",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "VersaoTermos",
                table: "Usuarios");
        }
    }
}
