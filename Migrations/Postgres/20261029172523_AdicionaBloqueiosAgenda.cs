using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaiFisio.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AdicionaBloqueiosAgenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloqueiosAgenda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueiosAgenda", x => x.Id);
                    table.CheckConstraint("CK_BloqueiosAgenda_Intervalo", "\"Fim\" > \"Inicio\"");
                    table.ForeignKey(
                        name: "FK_BloqueiosAgenda_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloqueiosAgenda_ProfissionalId_Inicio",
                table: "BloqueiosAgenda",
                columns: new[] { "ProfissionalId", "Inicio" });

            // Preserva os bloqueios criados pela versão anterior e remove o paciente técnico
            // que era usado para representá-los como consultas fictícias.
            migrationBuilder.Sql(
                """
                INSERT INTO "BloqueiosAgenda"
                    ("Id", "ProfissionalId", "Inicio", "Fim", "Tipo", "Motivo", "CriadoEm")
                SELECT
                    "Id",
                    "ProfissionalId",
                    "DataHora",
                    "DataHora" + INTERVAL '1 hour',
                    'Horario',
                    "Observacoes",
                    CURRENT_TIMESTAMP
                FROM "Agendamentos"
                WHERE "Status" = 'Bloqueado'
                ON CONFLICT ("Id") DO NOTHING;

                DELETE FROM "Agendamentos"
                WHERE "Status" = 'Bloqueado';

                DELETE FROM "Pacientes"
                WHERE "Id" = 'b10c0000-0000-4000-8000-000000000001';

                DELETE FROM "Enderecos"
                WHERE "Id" = 'b10c0000-0000-4000-8000-000000000003';

                DELETE FROM "Usuarios"
                WHERE "Id" = 'b10c0000-0000-4000-8000-000000000002';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloqueiosAgenda");
        }
    }
}
