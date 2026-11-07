using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AgendaiFisio.Repositorio.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InicialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Enderecos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rua = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Complemento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enderecos", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoUsuario = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeCompleto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataNascimento = table.Column<DateTime>(type: "date", nullable: false),
                    Sexo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstadoCivil = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnderecoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pacientes_Enderecos_EnderecoId",
                        column: x => x.EnderecoId,
                        principalTable: "Enderecos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pacientes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profissionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeCompleto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Crefito = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataNascimento = table.Column<DateTime>(type: "date", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    EspecialidadeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Especialidade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profissionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profissionais_Especialidades_EspecialidadeId",
                        column: x => x.EspecialidadeId,
                        principalTable: "Especialidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Profissionais_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Agendamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agendamentos_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Agendamentos_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AvaliacaoFisioterapeuta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueixaPrincipal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HistoriaPregressa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HabitosDeVida = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TratamentosRealizados = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AntecedentesPessoaisFamiliares = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataAvaliacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "BloqueiosAgenda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fim = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueiosAgenda", x => x.Id);
                    table.CheckConstraint("CK_BloqueiosAgenda_Intervalo", "[Fim] > [Inicio]");
                    table.ForeignKey(
                        name: "FK_BloqueiosAgenda_Profissionais_ProfissionalId",
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
                    Objetivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QtdSessoes = table.Column<int>(type: "int", nullable: false),
                    Procedimentos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvaliacaoFisioterapeutaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            migrationBuilder.InsertData(
                table: "Especialidades",
                columns: new[] { "Id", "Nome" },
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

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_PacienteId_DataHora",
                table: "Agendamentos",
                columns: new[] { "PacienteId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_ProfissionalId_DataHora",
                table: "Agendamentos",
                columns: new[] { "ProfissionalId", "DataHora" },
                unique: true,
                filter: "[Status] <> 'Cancelado'");

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacaoFisioterapeuta_PacienteId",
                table: "AvaliacaoFisioterapeuta",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacaoFisioterapeuta_ProfissionalId",
                table: "AvaliacaoFisioterapeuta",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueiosAgenda_ProfissionalId_Inicio",
                table: "BloqueiosAgenda",
                columns: new[] { "ProfissionalId", "Inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Especialidades_Nome",
                table: "Especialidades",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_EnderecoId",
                table: "Pacientes",
                column: "EnderecoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_UsuarioId",
                table: "Pacientes",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanosTerapeutico_AvaliacaoFisioterapeutaId",
                table: "PlanosTerapeutico",
                column: "AvaliacaoFisioterapeutaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_Crefito",
                table: "Profissionais",
                column: "Crefito",
                unique: true,
                filter: "[Crefito] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_EspecialidadeId",
                table: "Profissionais",
                column: "EspecialidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_UsuarioId",
                table: "Profissionais",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_Agendamentos_ValidarBloqueio]
                ON [Agendamentos]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        INNER JOIN [BloqueiosAgenda] b WITH (UPDLOCK, HOLDLOCK)
                            ON b.[ProfissionalId] = i.[ProfissionalId]
                           AND b.[Inicio] <= i.[DataHora]
                           AND b.[Fim] > i.[DataHora]
                        WHERE i.[Status] <> 'Cancelado')
                    BEGIN
                        THROW 51001, 'O horário está bloqueado pelo profissional.', 1;
                    END;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_BloqueiosAgenda_ValidarConsulta]
                ON [BloqueiosAgenda]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        INNER JOIN [Agendamentos] a WITH (UPDLOCK, HOLDLOCK)
                            ON a.[ProfissionalId] = i.[ProfissionalId]
                           AND a.[DataHora] >= i.[Inicio]
                           AND a.[DataHora] < i.[Fim]
                        WHERE a.[Status] <> 'Cancelado')
                    BEGIN
                        THROW 51002, 'O período possui uma consulta e não pode ser bloqueado.', 1;
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        INNER JOIN [BloqueiosAgenda] b WITH (UPDLOCK, HOLDLOCK)
                            ON b.[Id] <> i.[Id]
                           AND b.[ProfissionalId] = i.[ProfissionalId]
                           AND b.[Inicio] < i.[Fim]
                           AND b.[Fim] > i.[Inicio])
                    BEGIN
                        THROW 51003, 'O período informado já possui um bloqueio.', 1;
                    END;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [TR_Agendamentos_ValidarBloqueio];");
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [TR_BloqueiosAgenda_ValidarConsulta];");

            migrationBuilder.DropTable(
                name: "Agendamentos");

            migrationBuilder.DropTable(
                name: "BloqueiosAgenda");

            migrationBuilder.DropTable(
                name: "PlanosTerapeutico");

            migrationBuilder.DropTable(
                name: "AvaliacaoFisioterapeuta");

            migrationBuilder.DropTable(
                name: "Pacientes");

            migrationBuilder.DropTable(
                name: "Profissionais");

            migrationBuilder.DropTable(
                name: "Enderecos");

            migrationBuilder.DropTable(
                name: "Especialidades");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
