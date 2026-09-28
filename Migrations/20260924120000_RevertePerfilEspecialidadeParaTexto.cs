using System;
using AgendaiFisio.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaiFisio.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AgendaiFisioDbContext))]
    [Migration("20260924120000_RevertePerfilEspecialidadeParaTexto")]
    public partial class RevertePerfilEspecialidadeParaTexto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Volta a coluna de texto, já preenchida com o nome da especialidade vinculada.
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

            migrationBuilder.DropForeignKey(
                name: "FK_Profissionais_Especialidades_EspecialidadeId",
                table: "Profissionais");

            migrationBuilder.DropIndex(
                name: "IX_Profissionais_EspecialidadeId",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "EspecialidadeId",
                table: "Profissionais");

            // A tabela Especialidades continua existindo, como catálogo de referência (sem vínculo obrigatório).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EspecialidadeId",
                table: "Profissionais",
                type: "uniqueidentifier",
                nullable: true);

            // Recria uma especialidade para cada texto ainda não catalogado e reconecta os profissionais.
            migrationBuilder.Sql(@"
                INSERT INTO Especialidades (Id, Nome)
                SELECT NEWID(), LEFT(LTRIM(RTRIM(MIN(Especialidade))), 100)
                FROM Profissionais
                WHERE LTRIM(RTRIM(Especialidade)) <> ''
                    AND NOT EXISTS (
                        SELECT 1 FROM Especialidades e WHERE e.Nome = LEFT(LTRIM(RTRIM(Profissionais.Especialidade)), 100)
                    )
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
    }
}
