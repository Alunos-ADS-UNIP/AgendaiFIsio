using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaiFisio.Repositorio.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class RemoveTabelasClinicasNaoUtilizadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanosTerapeutico");

            migrationBuilder.DropTable(
                name: "AvaliacaoFisioterapeuta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvaliacaoFisioterapeuta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AntecedentesPessoaisFamiliares = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataAvaliacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HabitosDeVida = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HistoriaPregressa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QueixaPrincipal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TratamentosRealizados = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaliacaoFisioterapeuta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaliacaoFisioterapeuta_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AvaliacaoFisioterapeuta_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanosTerapeutico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvaliacaoFisioterapeutaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Objetivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Procedimentos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QtdSessoes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosTerapeutico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanosTerapeutico_AvaliacaoFisioterapeuta_AvaliacaoFisioterapeutaId",
                        column: x => x.AvaliacaoFisioterapeutaId,
                        principalTable: "AvaliacaoFisioterapeuta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacaoFisioterapeuta_PacienteId",
                table: "AvaliacaoFisioterapeuta",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacaoFisioterapeuta_ProfissionalId",
                table: "AvaliacaoFisioterapeuta",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosTerapeutico_AvaliacaoFisioterapeutaId",
                table: "PlanosTerapeutico",
                column: "AvaliacaoFisioterapeutaId",
                unique: true);
        }
    }
}
