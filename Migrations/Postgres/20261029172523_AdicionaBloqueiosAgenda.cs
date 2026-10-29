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

            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS btree_gist;

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
                WHERE "Id" = 'b10c0000-0000-4000-8000-000000000001'
                  AND NOT EXISTS (
                      SELECT 1 FROM "Agendamentos"
                      WHERE "PacienteId" = 'b10c0000-0000-4000-8000-000000000001'
                  );

                DELETE FROM "Enderecos"
                WHERE "Id" = 'b10c0000-0000-4000-8000-000000000003'
                  AND NOT EXISTS (
                      SELECT 1 FROM "Pacientes"
                      WHERE "EnderecoId" = 'b10c0000-0000-4000-8000-000000000003'
                  );

                DELETE FROM "Usuarios"
                WHERE "Id" = 'b10c0000-0000-4000-8000-000000000002'
                  AND NOT EXISTS (
                      SELECT 1 FROM "Pacientes"
                      WHERE "UsuarioId" = 'b10c0000-0000-4000-8000-000000000002'
                  );

                ALTER TABLE "BloqueiosAgenda"
                ADD CONSTRAINT "EX_BloqueiosAgenda_Sobreposicao"
                EXCLUDE USING gist (
                    "ProfissionalId" WITH =,
                    tstzrange("Inicio", "Fim", '[)') WITH &&
                );

                CREATE OR REPLACE FUNCTION "ValidarAgendamentoSemBloqueio"()
                RETURNS trigger AS $$
                BEGIN
                    IF NEW."Status" <> 'Cancelado' THEN
                        PERFORM pg_advisory_xact_lock(
                            hashtextextended(NEW."ProfissionalId"::text, 0));

                        IF EXISTS (
                            SELECT 1
                            FROM "BloqueiosAgenda" b
                            WHERE b."ProfissionalId" = NEW."ProfissionalId"
                              AND b."Inicio" <= NEW."DataHora"
                              AND b."Fim" > NEW."DataHora"
                        ) THEN
                            RAISE EXCEPTION 'O horário está bloqueado pelo profissional.'
                                USING ERRCODE = '23P01',
                                      CONSTRAINT = 'CK_Agendamentos_SemBloqueio';
                        END IF;
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE OR REPLACE FUNCTION "ValidarBloqueioSemConsulta"()
                RETURNS trigger AS $$
                BEGIN
                    PERFORM pg_advisory_xact_lock(
                        hashtextextended(NEW."ProfissionalId"::text, 0));

                    IF EXISTS (
                        SELECT 1
                        FROM "Agendamentos" a
                        WHERE a."ProfissionalId" = NEW."ProfissionalId"
                          AND a."Status" <> 'Cancelado'
                          AND a."DataHora" >= NEW."Inicio"
                          AND a."DataHora" < NEW."Fim"
                    ) THEN
                        RAISE EXCEPTION 'O período possui uma consulta e não pode ser bloqueado.'
                            USING ERRCODE = '23P01',
                                  CONSTRAINT = 'CK_BloqueiosAgenda_SemConsulta';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "BloqueiosAgenda" b
                        WHERE b."Id" <> NEW."Id"
                          AND b."ProfissionalId" = NEW."ProfissionalId"
                          AND b."Inicio" < NEW."Fim"
                          AND b."Fim" > NEW."Inicio"
                    ) THEN
                        RAISE EXCEPTION 'O período informado já possui um bloqueio.'
                            USING ERRCODE = '23P01',
                                  CONSTRAINT = 'EX_BloqueiosAgenda_Sobreposicao';
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER "TR_Agendamentos_ValidarBloqueio"
                BEFORE INSERT OR UPDATE OF "ProfissionalId", "DataHora", "Status"
                ON "Agendamentos"
                FOR EACH ROW
                EXECUTE FUNCTION "ValidarAgendamentoSemBloqueio"();

                CREATE TRIGGER "TR_BloqueiosAgenda_ValidarConsulta"
                BEFORE INSERT OR UPDATE OF "ProfissionalId", "Inicio", "Fim"
                ON "BloqueiosAgenda"
                FOR EACH ROW
                EXECUTE FUNCTION "ValidarBloqueioSemConsulta"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_Agendamentos_ValidarBloqueio"
                    ON "Agendamentos";
                DROP TRIGGER IF EXISTS "TR_BloqueiosAgenda_ValidarConsulta"
                    ON "BloqueiosAgenda";
                DROP FUNCTION IF EXISTS "ValidarAgendamentoSemBloqueio"();
                DROP FUNCTION IF EXISTS "ValidarBloqueioSemConsulta"();

                INSERT INTO "Enderecos"
                    ("Id", "Bairro", "Cep", "Cidade", "Complemento", "Estado", "Numero", "Rua")
                SELECT
                    'b10c0000-0000-4000-8000-000000000003', '', '', '', '', '', '', ''
                WHERE EXISTS (SELECT 1 FROM "BloqueiosAgenda")
                ON CONFLICT DO NOTHING;

                INSERT INTO "Usuarios"
                    ("Id", "Email", "SenhaHash", "TipoUsuario")
                SELECT
                    'b10c0000-0000-4000-8000-000000000002',
                    'bloqueio-agenda@agendaifisio.invalid',
                    'conta-tecnica-sem-login',
                    'Sistema'
                WHERE EXISTS (SELECT 1 FROM "BloqueiosAgenda")
                ON CONFLICT DO NOTHING;

                INSERT INTO "Pacientes"
                    ("Id", "Cpf", "DataNascimento", "EnderecoId", "EstadoCivil",
                     "NomeCompleto", "Sexo", "Telefone", "UsuarioId")
                SELECT
                    'b10c0000-0000-4000-8000-000000000001',
                    '', DATE '2000-01-01',
                    'b10c0000-0000-4000-8000-000000000003', '',
                    'Bloqueio de agenda', '', '',
                    'b10c0000-0000-4000-8000-000000000002'
                WHERE EXISTS (SELECT 1 FROM "BloqueiosAgenda")
                ON CONFLICT DO NOTHING;

                INSERT INTO "Agendamentos"
                    ("Id", "PacienteId", "ProfissionalId", "DataHora", "Status", "Observacoes")
                SELECT
                    "Id",
                    'b10c0000-0000-4000-8000-000000000001',
                    "ProfissionalId",
                    "Inicio",
                    'Bloqueado',
                    "Motivo"
                FROM "BloqueiosAgenda"
                ON CONFLICT ("Id") DO NOTHING;
                """);

            migrationBuilder.DropTable(
                name: "BloqueiosAgenda");
        }
    }
}
