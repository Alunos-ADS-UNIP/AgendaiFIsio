using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

var connectionString = Environment.GetEnvironmentVariable("MIGRATION_TEST_CONNECTION_STRING")
    ?? throw new InvalidOperationException(
        "Configure MIGRATION_TEST_CONNECTION_STRING com um SQL Server descartável.");

if (!string.Equals(
        Environment.GetEnvironmentVariable("MIGRATION_TEST_ALLOW_RESET"),
        "true",
        StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "MIGRATION_TEST_ALLOW_RESET=true é obrigatório: a suíte recria o banco informado.");
}

var options = new DbContextOptionsBuilder<AgendaiFisioDbContext>()
    .UseSqlServer(connectionString)
    .Options;
var checks = 0;

void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}

async Task<bool> TableExistsAsync(string table)
{
    await using var context = new AgendaiFisioDbContext(options);
    await using var command = context.Database.GetDbConnection().CreateCommand();
    command.CommandText = "SELECT CASE WHEN OBJECT_ID(@table, 'U') IS NULL THEN 0 ELSE 1 END";
    var parameter = command.CreateParameter();
    parameter.ParameterName = "table";
    parameter.Value = "dbo." + table;
    command.Parameters.Add(parameter);
    await context.Database.OpenConnectionAsync();
    return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
}

async Task ExpectSqlErrorAsync(Func<Task> action, int number, string name)
{
    try
    {
        await action();
        Check(false, name);
    }
    catch (DbUpdateException ex) when (
        ex.InnerException is SqlException sqlException && sqlException.Number == number)
    {
        Check(true, name);
    }
}

await using (var reset = new AgendaiFisioDbContext(options))
    await reset.Database.EnsureDeletedAsync();

try
{
    await using (var context = new AgendaiFisioDbContext(options))
    {
        await context.Database.MigrateAsync();
        Check(await context.Database.CanConnectAsync(), "Banco SQL Server conectado");
        Check(!(await context.Database.GetPendingMigrationsAsync()).Any(),
            "Banco sem migrations pendentes");
        Check(await context.Especialidades.CountAsync() == 16,
            "Catálogo inicial contém 16 especialidades");
    }

    Check(await TableExistsAsync("BloqueiosAgenda"),
        "Migration inicial cria BloqueiosAgenda");

    var profissionalId = Guid.NewGuid();
    var horario = DateTime.UtcNow.Date.AddDays(20).AddHours(12);

    await using (var context = new AgendaiFisioDbContext(options))
    {
        var profissional = new Profissional
        {
            Id = profissionalId,
            Usuario = new Usuario
            {
                Email = "migration-professional@example.invalid",
                SenhaHash = "test",
                TipoUsuario = "Profissional"
            },
            NomeCompleto = "Profissional Migration",
            Crefito = "MIGRATION-1",
            DataNascimento = new DateTime(1990, 1, 1),
            DataCadastro = DateTime.UtcNow,
            Ativo = true
        };
        var paciente = new Paciente
        {
            Usuario = new Usuario
            {
                Email = "migration-patient@example.invalid",
                SenhaHash = "test",
                TipoUsuario = "Paciente"
            },
            Endereco = new Endereco(),
            NomeCompleto = "Paciente Migration",
            DataNascimento = new DateTime(1990, 1, 1)
        };

        context.AddRange(profissional, paciente);
        await context.SaveChangesAsync();

        context.BloqueiosAgenda.Add(new BloqueioAgenda
        {
            ProfissionalId = profissionalId,
            Inicio = horario,
            Fim = horario.AddHours(2),
            Tipo = "Horario",
            CriadoEm = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await ExpectSqlErrorAsync(async () =>
        {
            context.Agendamentos.Add(new Agendamento
            {
                PacienteId = paciente.Id,
                ProfissionalId = profissionalId,
                DataHora = horario.AddMinutes(30),
                Status = "Agendado"
            });
            await context.SaveChangesAsync();
        }, 51001, "Trigger impede consulta dentro de bloqueio");
    }

    await using (var context = new AgendaiFisioDbContext(options))
    {
        context.BloqueiosAgenda.RemoveRange(context.BloqueiosAgenda);
        await context.SaveChangesAsync();

        var pacienteId = await context.Pacientes.Select(p => p.Id).SingleAsync();
        context.Agendamentos.Add(new Agendamento
        {
            PacienteId = pacienteId,
            ProfissionalId = profissionalId,
            DataHora = horario,
            Status = "Agendado"
        });
        await context.SaveChangesAsync();

        await ExpectSqlErrorAsync(async () =>
        {
            context.BloqueiosAgenda.Add(new BloqueioAgenda
            {
                ProfissionalId = profissionalId,
                Inicio = horario.AddMinutes(-30),
                Fim = horario.AddMinutes(30),
                Tipo = "Horario",
                CriadoEm = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }, 51002, "Trigger impede bloqueio sobre consulta");
    }

    await using (var context = new AgendaiFisioDbContext(options))
    {
        var inicio = horario.AddDays(2);
        context.BloqueiosAgenda.Add(new BloqueioAgenda
        {
            ProfissionalId = profissionalId,
            Inicio = inicio,
            Fim = inicio.AddHours(2),
            Tipo = "Horario",
            CriadoEm = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await ExpectSqlErrorAsync(async () =>
        {
            context.BloqueiosAgenda.Add(new BloqueioAgenda
            {
                ProfissionalId = profissionalId,
                Inicio = inicio.AddHours(1),
                Fim = inicio.AddHours(3),
                Tipo = "Horario",
                CriadoEm = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }, 51003, "Trigger impede bloqueios sobrepostos");
    }

    await using (var context = new AgendaiFisioDbContext(options))
        await context.Database.GetService<IMigrator>().MigrateAsync("0");

    Check(!await TableExistsAsync("BloqueiosAgenda"),
        "Rollback remove as tabelas da migration inicial");
}
finally
{
    await using var cleanup = new AgendaiFisioDbContext(options);
    await cleanup.Database.EnsureDeletedAsync();
}

Console.WriteLine($"ALL TESTS PASSED ({checks} checks)");
