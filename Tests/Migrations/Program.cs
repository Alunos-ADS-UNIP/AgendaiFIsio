using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using AgendaiFisio.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

const string InitialMigration = "20261023231012_InitialPostgres";
var databaseUrl = Environment.GetEnvironmentVariable("MIGRATION_TEST_DATABASE_URL")
    ?? throw new InvalidOperationException(
        "Configure MIGRATION_TEST_DATABASE_URL com um PostgreSQL descartável.");
if (!string.Equals(
        Environment.GetEnvironmentVariable("MIGRATION_TEST_ALLOW_RESET"),
        "true",
        StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "MIGRATION_TEST_ALLOW_RESET=true é obrigatório: a suíte recria o schema public.");
}
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["MIGRATION_TEST_DATABASE_URL"] = databaseUrl
    })
    .Build();
var connectionString = PostgresConnectionString.Resolve(
    configuration,
    "MIGRATION_TEST_DATABASE_URL");

var options = new DbContextOptionsBuilder<AgendaiFisioDbContext>()
    .UseNpgsql(connectionString)
    .Options;
var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}

async Task ResetSchemaAsync()
{
    await using var context = new AgendaiFisioDbContext(options);
    await context.Database.ExecuteSqlRawAsync(
        "DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;");
}

async Task<bool> TableExistsAsync(string table)
{
    await using var context = new AgendaiFisioDbContext(options);
    await using var command = context.Database.GetDbConnection().CreateCommand();
    command.CommandText =
        "SELECT to_regclass('public.' || quote_ident(@table)) IS NOT NULL";
    var parameter = command.CreateParameter();
    parameter.ParameterName = "table";
    parameter.Value = table;
    command.Parameters.Add(parameter);
    await context.Database.OpenConnectionAsync();
    return (bool)(await command.ExecuteScalarAsync())!;
}

var technicalPatientId = Guid.Parse("b10c0000-0000-4000-8000-000000000001");
var technicalUserId = Guid.Parse("b10c0000-0000-4000-8000-000000000002");
var technicalAddressId = Guid.Parse("b10c0000-0000-4000-8000-000000000003");
var professionalId = Guid.NewGuid();
var professionalUserId = Guid.NewGuid();
var realPatientId = Guid.NewGuid();
var realUserId = Guid.NewGuid();
var realAddressId = Guid.NewGuid();
var oldBlockId = Guid.NewGuid();
var realAppointmentId = Guid.NewGuid();
var oldBlockTime = DateTime.UtcNow.Date.AddDays(20).AddHours(9);

try
{
    // Cenário 1: instalação e rollback em banco vazio.
    await ResetSchemaAsync();
    await using (var context = new AgendaiFisioDbContext(options))
    {
        await context.Database.MigrateAsync();
        Check(await TableExistsAsync("BloqueiosAgenda"),
            "Migration aplica em banco vazio");
        await context.Database.GetService<IMigrator>().MigrateAsync(InitialMigration);
        Check(!await TableExistsAsync("BloqueiosAgenda"),
            "Rollback remove a tabela somente após executar o Down");
        Check(!await context.Pacientes.AnyAsync(p => p.Id == technicalPatientId),
            "Rollback vazio não cria paciente técnico sem necessidade");
    }

    // Cenário 2: dados do fluxo antigo, incluindo um paciente real que deve permanecer intacto.
    await ResetSchemaAsync();
    await using (var context = new AgendaiFisioDbContext(options))
    {
        await context.Database.GetService<IMigrator>().MigrateAsync(InitialMigration);

        var technicalUser = new Usuario
        {
            Id = technicalUserId,
            Email = "bloqueio-agenda@agendaifisio.invalid",
            SenhaHash = "conta-tecnica-sem-login",
            TipoUsuario = "Sistema"
        };
        var technicalAddress = new Endereco { Id = technicalAddressId };
        var technicalPatient = new Paciente
        {
            Id = technicalPatientId,
            NomeCompleto = "Bloqueio de agenda",
            DataNascimento = new DateTime(2000, 1, 1),
            UsuarioId = technicalUserId,
            Usuario = technicalUser,
            Endereco = technicalAddress
        };
        var professionalUser = new Usuario
        {
            Id = professionalUserId,
            Email = "migration-professional@example.invalid",
            SenhaHash = "test",
            TipoUsuario = "Profissional"
        };
        var professional = new Profissional
        {
            Id = professionalId,
            NomeCompleto = "Profissional Migration",
            Crefito = "MIGRATION-1",
            DataNascimento = new DateTime(1990, 1, 1),
            DataCadastro = DateTime.UtcNow,
            Ativo = true,
            UsuarioId = professionalUserId,
            Usuario = professionalUser
        };
        var realUser = new Usuario
        {
            Id = realUserId,
            Email = "migration-patient@example.invalid",
            SenhaHash = "test",
            TipoUsuario = "Paciente"
        };
        var realAddress = new Endereco { Id = realAddressId, Rua = "Rua real" };
        var realPatient = new Paciente
        {
            Id = realPatientId,
            NomeCompleto = "Paciente Real",
            Cpf = "52998224725",
            DataNascimento = new DateTime(1991, 1, 1),
            UsuarioId = realUserId,
            Usuario = realUser,
            Endereco = realAddress
        };
        context.AddRange(technicalPatient, professional, realPatient);
        context.Agendamentos.AddRange(
            new Agendamento
            {
                Id = oldBlockId,
                PacienteId = technicalPatientId,
                ProfissionalId = professionalId,
                DataHora = oldBlockTime,
                Status = "Bloqueado",
                Observacoes = "Reunião externa"
            },
            new Agendamento
            {
                Id = realAppointmentId,
                PacienteId = realPatientId,
                ProfissionalId = professionalId,
                DataHora = oldBlockTime.AddHours(2),
                Status = "Agendado",
                Observacoes = "Consulta real"
            });
        await context.SaveChangesAsync();
        await context.Database.MigrateAsync();
    }

    await using (var context = new AgendaiFisioDbContext(options))
    {
        var converted = await context.BloqueiosAgenda.SingleAsync(b => b.Id == oldBlockId);
        Check(converted.ProfissionalId == professionalId &&
              converted.Inicio == oldBlockTime &&
              converted.Fim == oldBlockTime.AddHours(1) &&
              converted.Motivo == "Reunião externa",
            "Migration preserva profissional, horário, duração e motivo do bloqueio antigo");
        Check(!await context.Agendamentos.AnyAsync(a => a.Id == oldBlockId),
            "Agendamento técnico convertido é removido");
        Check(!await context.Pacientes.AnyAsync(p => p.Id == technicalPatientId) &&
              !await context.Usuarios.AnyAsync(u => u.Id == technicalUserId) &&
              !await context.Enderecos.AnyAsync(e => e.Id == technicalAddressId),
            "Somente paciente, usuário e endereço técnicos são removidos");
        Check(await context.Pacientes.AnyAsync(p => p.Id == realPatientId) &&
              await context.Agendamentos.AnyAsync(a => a.Id == realAppointmentId),
            "Paciente e consulta reais são preservados");

        await context.Database.GetService<IMigrator>().MigrateAsync(InitialMigration);
        Check(!await TableExistsAsync("BloqueiosAgenda"),
            "Rollback legado remove a tabela depois de restaurar os dados");
        Check(await context.Pacientes.AnyAsync(p => p.Id == technicalPatientId) &&
              await context.Usuarios.AnyAsync(u => u.Id == technicalUserId) &&
              await context.Enderecos.AnyAsync(e => e.Id == technicalAddressId),
            "Rollback restaura os registros técnicos necessários");
        var restored = await context.Agendamentos.SingleAsync(a => a.Id == oldBlockId);
        Check(restored.Status == "Bloqueado" &&
              restored.ProfissionalId == professionalId &&
              restored.DataHora == oldBlockTime &&
              restored.Observacoes == "Reunião externa",
            "Rollback restaura o bloqueio no contrato antigo sem perda silenciosa");
        Check(await context.Pacientes.AnyAsync(p => p.Id == realPatientId),
            "Rollback preserva paciente real");
    }
}
finally
{
    await ResetSchemaAsync();
}

Console.WriteLine($"{checks} verificações de migration aprovadas.");
