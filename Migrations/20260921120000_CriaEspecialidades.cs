using System;
using AgendaiFisio.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaiFisio.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AgendaiFisioDbContext))]
    [Migration("20260921120000_CriaEspecialidades")]
    public partial class CriaEspecialidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Especialidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Especialidades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Especialidades_Nome",
                table: "Especialidades",
                column: "Nome",
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EspecialidadeId",
                table: "Profissionais",
                type: "uniqueidentifier",
                nullable: true);

            // Aproveita os textos já digitados: cria uma especialidade para cada nome distinto e vincula os profissionais.
            migrationBuilder.Sql(@"
                INSERT INTO Especialidades (Id, Nome)
                SELECT NEWID(), LEFT(LTRIM(RTRIM(MIN(Especialidade))), 100)
                FROM Profissionais
                WHERE LTRIM(RTRIM(Especialidade)) <> ''
                GROUP BY LEFT(LTRIM(RTRIM(Especialidade)), 100);

                UPDATE p
                SET p.EspecialidadeId = e.Id
                FROM Profissionais p
                INNER JOIN Especialidades e ON e.Nome = LEFT(LTRIM(RTRIM(p.Especialidade)), 100);
            ");

            migrationBuilder.DropColumn(
                name: "Especialidade",
                table: "Profissionais");

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_EspecialidadeId",
                table: "Profissionais",
                column: "EspecialidadeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Profissionais_Especialidades_EspecialidadeId",
                table: "Profissionais",
                column: "EspecialidadeId",
                principalTable: "Especialidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Profissionais_Especialidades_EspecialidadeId",
                table: "Profissionais");

            migrationBuilder.DropIndex(
                name: "IX_Profissionais_EspecialidadeId",
                table: "Profissionais");

            migrationBuilder.AddColumn<string>(
                name: "Especialidade",
                table: "Profissionais",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE p
                SET p.Especialidade = e.Nome
                FROM Profissionais p
                INNER JOIN Especialidades e ON e.Id = p.EspecialidadeId;
            ");

            migrationBuilder.DropColumn(
                name: "EspecialidadeId",
                table: "Profissionais");

            migrationBuilder.DropTable(
                name: "Especialidades");
        }
    }
}
