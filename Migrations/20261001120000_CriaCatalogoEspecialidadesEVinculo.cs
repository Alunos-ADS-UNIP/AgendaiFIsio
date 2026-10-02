using System;
using AgendaiFisio.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaiFisio.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AgendaiFisioDbContext))]
    [Migration("20261001120000_CriaCatalogoEspecialidadesEVinculo")]
    public partial class CriaCatalogoEspecialidadesEVinculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Catálogo de especialidades -------------------------------------------------
            // A collation Latin1_General_CI_AI faz comparações e o índice único ignorarem
            // maiúsculas/minúsculas e acentos (ex.: "Respiratória" == "respiratoria").
            migrationBuilder.CreateTable(
                name: "Especialidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_CI_AI")
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

            // Lista inicial aprovada (ver docs/especialidades.md). IDs fixos: reexecutar esta
            // migration em outro ambiente nunca gera um ID diferente para o mesmo nome.
            migrationBuilder.InsertData(
                table: "Especialidades",
                columns: new[] { "Id", "Nome" },
                columnTypes: new[] { "uniqueidentifier", "nvarchar(100)" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "Acupuntura" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "Aquática" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "Cardiovascular" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "Dermatofuncional" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), "Fisioterapia do Trabalho" },
                    { new Guid("00000000-0000-0000-0000-000000000006"), "Esportiva" },
                    { new Guid("00000000-0000-0000-0000-000000000007"), "Gerontologia" },
                    { new Guid("00000000-0000-0000-0000-000000000008"), "Neurofuncional" },
                    { new Guid("00000000-0000-0000-0000-000000000009"), "Oncologia" },
                    { new Guid("00000000-0000-0000-0000-000000000010"), "Osteopatia" },
                    { new Guid("00000000-0000-0000-0000-000000000011"), "Quiropraxia" },
                    { new Guid("00000000-0000-0000-0000-000000000012"), "Reumatologia" },
                    { new Guid("00000000-0000-0000-0000-000000000013"), "Fisioterapia Respiratória" },
                    { new Guid("00000000-0000-0000-0000-000000000014"), "Fisioterapia em Saúde da Mulher" },
                    { new Guid("00000000-0000-0000-0000-000000000015"), "Traumato-Ortopédica" },
                    { new Guid("00000000-0000-0000-0000-000000000016"), "Terapia Intensiva" }
                });

            // --- Vínculo opcional do profissional --------------------------------------------
            migrationBuilder.AddColumn<Guid>(
                name: "EspecialidadeId",
                table: "Profissionais",
                type: "uniqueidentifier",
                nullable: true);

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

            // --- Associação dos textos legados ao catálogo -----------------------------------
            // Casa a coluna antiga (texto livre) com o catálogo usando a mesma regra de
            // comparação (aparar as pontas + ignorar caixa/acento via COLLATE inline). Só liga
            // quando o nome bate exatamente com um item aprovado; não cria especialidade nova
            // nem adivinha equivalência semântica (ex.: "Ortopedia" não vira "Traumato-Ortopédica"
            // e "Respiratória" não vira "Fisioterapia Respiratória" — isso exige mapeamento
            // humano revisado, fora do escopo desta migration).
            //
            // A coluna antiga "Especialidade" é preservada de propósito (ver Profissional.
            // EspecialidadeTextoLegado) para que o texto original de quem não deu match continue
            // disponível para revisão. Para listar as divergências pendentes depois de aplicar
            // esta migration, rode:
            //   SELECT Id, Especialidade FROM Profissionais
            //   WHERE EspecialidadeId IS NULL AND LTRIM(RTRIM(Especialidade)) <> '';
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.EspecialidadeId = e.Id
                FROM Profissionais p
                INNER JOIN Especialidades e
                    ON LTRIM(RTRIM(p.Especialidade)) COLLATE Latin1_General_CI_AI = e.Nome COLLATE Latin1_General_CI_AI
                WHERE LTRIM(RTRIM(p.Especialidade)) <> '';
            ");
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

            migrationBuilder.DropColumn(
                name: "EspecialidadeId",
                table: "Profissionais");

            migrationBuilder.DropTable(
                name: "Especialidades");
        }
    }
}
