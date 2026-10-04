using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AgendaiFisio.Context;
using AgendaiFisio.Services.Paciente;
using AgendaiFisio.Validations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Executar da raiz do repositório, com a API em Development já iniciada.
var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json")
    .AddEnvironmentVariables().Build();
var adminEmail = config["RegressionAdmin:Email"]
    ?? throw new InvalidOperationException("Configure RegressionAdmin__Email para executar os testes administrativos.");
var adminSenha = config["RegressionAdmin:Senha"]
    ?? throw new InvalidOperationException("Configure RegressionAdmin__Senha para executar os testes administrativos.");
await using var db = new AgendaiFisioDbContext(new DbContextOptionsBuilder<AgendaiFisioDbContext>()
    .UseSqlServer(config.GetConnectionString("AgendaiFisioDbContext")).Options);
using var http = new HttpClient { BaseAddress = new Uri(args.FirstOrDefault() ?? "http://localhost:5187") };
var prefix = "regression-" + Guid.NewGuid().ToString("N");
var emails = new[] { prefix + "-pac@example.invalid", prefix + "-prof@example.invalid", prefix + "-invalid@example.invalid" };
Guid? especialidadeCriadaId = null;
var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}
async Task<JsonElement> Request(HttpMethod method, string path, object? body, HttpStatusCode expected, string name, string? token = null)
{
    using var request = new HttpRequestMessage(method, path);
    if (body is not null) request.Content = JsonContent.Create(body);
    if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    using var response = await http.SendAsync(request);
    Check(response.StatusCode == expected, name + " (HTTP " + (int)response.StatusCode + ")");
    var text = await response.Content.ReadAsStringAsync();
    return string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
}
object Patient(string cpf, bool address) => address
    ? new { nomeCompleto = prefix, cpf, dataNascimento = "1990-01-01", rua = "Rua de teste", numero = "10", cep = "01001000", complemento = "Sala 1", bairro = "Centro", cidade = "Sao Paulo", estado = "SP" }
    : new { nomeCompleto = prefix, cpf, dataNascimento = "1990-01-01", rua = "Rua de teste", numero = "11", cep = "01001000" };
object Professional(string cpf) => new { nomeCompleto = prefix, cpf, crefito = "T" + Guid.NewGuid().ToString("N")[..12], telefone = "11999999999", dataNascimento = "1990-01-01", bio = "Teste automatizado", ativo = true };
Check(await db.Database.CanConnectAsync(), "Banco conectado");
Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "Banco sem migracoes pendentes");
try
{
    var cpfValidator = new CpfValidoAttribute();
    foreach (var cpf in new[] { "11111111111", "123", "abcdefghijk", "52998224724" })
        Check(!cpfValidator.IsValid(cpf), "CPF invalido rejeitado: " + cpf);
    Check(cpfValidator.IsValid("52998224725") && cpfValidator.IsValid("529.982.247-25"), "CPF valido com e sem pontuacao");
    Check(await new PacienteService(db).GetPacienteByIdAsync(Guid.NewGuid()) is null, "Paciente inexistente retorna null");
    await Request(HttpMethod.Get, "/openapi/v1.json", null, HttpStatusCode.OK, "OpenAPI com pacote atualizado");
    await Request(HttpMethod.Get, "/api/profissional", null, HttpStatusCode.Unauthorized, "Listagem exige autenticacao");
    foreach (var role in new[] { "Admin", "Clinica", "Desconhecido", "" })
        await Request(HttpMethod.Post, "/api/auth/register", new { email = emails[2], senha = "Regression123!", tipoUsuario = role }, HttpStatusCode.BadRequest, "Cadastro rejeita papel " + role);
    for (var i = 0; i < 2; i++)
    {
        var result = await Request(HttpMethod.Post, "/api/auth/register", new { email = emails[i], senha = "Regression123!", tipoUsuario = i == 0 ? " paciente " : "pRoFiSsIoNaL" }, HttpStatusCode.OK, "Cadastro com normalizacao");
        Check(result.GetProperty("dados").GetProperty("tipoUsuario").GetString() == (i == 0 ? "Paciente" : "Profissional"), "Papel canonico persistido");
    }
    async Task<string> Login(int index)
    {
        var result = await Request(HttpMethod.Post, "/api/auth/login", new { email = emails[index], senha = "Regression123!" }, HttpStatusCode.OK, "Login");
        return result.GetProperty("token").GetString()!;
    }
    var patientToken = await Login(0);
    var professionalToken = await Login(1);
    var adminLogin = await Request(HttpMethod.Post, "/api/auth/login", new { email = adminEmail, senha = adminSenha }, HttpStatusCode.OK, "Login da Clinica");
    var adminToken = adminLogin.GetProperty("token").GetString()!;
    var especialidades = await Request(HttpMethod.Get, "/api/especialidade", null, HttpStatusCode.OK, "Catalogo de especialidades publico");
    var especialidadeId = especialidades.EnumerateArray().First().GetProperty("id").GetGuid();
    await Request(HttpMethod.Get, "/api/especialidade/" + especialidadeId, null, HttpStatusCode.OK, "Consulta publica de especialidade");
    await Request(HttpMethod.Get, "/api/especialidade/" + Guid.NewGuid(), null, HttpStatusCode.NotFound, "Especialidade inexistente");
    await Request(HttpMethod.Post, "/api/especialidade", new { nome = "Especialidade sem autenticacao" }, HttpStatusCode.Unauthorized, "Cadastro de especialidade exige autenticacao");
    await Request(HttpMethod.Post, "/api/especialidade", new { nome = "Especialidade por profissional" }, HttpStatusCode.Forbidden, "Profissional nao cadastra especialidade", professionalToken);
    await Request(HttpMethod.Post, "/api/especialidade", new { nome = " x " }, HttpStatusCode.BadRequest, "Clinica nao cadastra nome curto", adminToken);
    var nomeEspecialidadeCriada = "Regressao " + prefix;
    var especialidadeCriada = await Request(HttpMethod.Post, "/api/especialidade", new { nome = "  " + nomeEspecialidadeCriada + "  " }, HttpStatusCode.Created, "Clinica cadastra especialidade", adminToken);
    especialidadeCriadaId = especialidadeCriada.GetProperty("id").GetGuid();
    Check(especialidadeCriada.GetProperty("nome").GetString() == nomeEspecialidadeCriada, "Nome da especialidade normalizado");
    await Request(HttpMethod.Post, "/api/especialidade", new { nome = nomeEspecialidadeCriada.ToUpperInvariant() }, HttpStatusCode.Conflict, "Catalogo rejeita especialidade duplicada", adminToken);
    await Request(HttpMethod.Post, "/api/auth/login", new { email = emails[0], senha = "errada" }, HttpStatusCode.Unauthorized, "Senha incorreta rejeitada");
    await Request(HttpMethod.Put, "/api/paciente/completar-perfil", Patient("11111111111", true), HttpStatusCode.BadRequest, "Rota paciente rejeita CPF invalido", patientToken);
    await Request(HttpMethod.Put, "/api/profissional/completar-perfil", Professional("11111111111"), HttpStatusCode.BadRequest, "Rota profissional rejeita CPF invalido", professionalToken);
    await Request(HttpMethod.Put, "/api/profissional/completar-perfil", Professional("52998224725"), HttpStatusCode.Forbidden, "Paciente nao altera profissional", patientToken);
    await Request(HttpMethod.Put, "/api/paciente/completar-perfil", Patient("52998224725", true), HttpStatusCode.Forbidden, "Profissional nao altera paciente", professionalToken);
    await Request(HttpMethod.Put, "/api/paciente/completar-perfil", Patient("52998224725", true), HttpStatusCode.OK, "Paciente preenche endereco", patientToken);
    await Request(HttpMethod.Put, "/api/paciente/completar-perfil", Patient("52998224725", false), HttpStatusCode.OK, "Paciente atualiza omitindo campos opcionais", patientToken);
    var patient = await db.Pacientes.AsNoTracking().Include(p => p.Endereco).SingleAsync(p => p.Usuario.Email == emails[0]);
    Check(patient.Endereco.Complemento == "Sala 1" && patient.Endereco.Bairro == "Centro" && patient.Endereco.Cidade == "Sao Paulo" && patient.Endereco.Estado == "SP" && patient.Endereco.Numero == "11", "Endereco preservado no banco");
    await Request(HttpMethod.Put, "/api/profissional/completar-perfil", Professional("52998224725"), HttpStatusCode.OK, "Profissional atualiza perfil", professionalToken);
    await Request(HttpMethod.Put, "/api/profissional/especialidade", new { especialidadeId = (Guid?)null }, HttpStatusCode.BadRequest, "Especialidade nula rejeitada", professionalToken);
    await Request(HttpMethod.Put, "/api/profissional/especialidade", new { especialidadeId = Guid.NewGuid() }, HttpStatusCode.BadRequest, "Especialidade inexistente rejeitada", professionalToken);
    await Request(HttpMethod.Put, "/api/profissional/especialidade", new { especialidadeId }, HttpStatusCode.OK, "Profissional escolhe especialidade apos cadastro", professionalToken);
    var professional = await db.Profissionais.AsNoTracking().SingleAsync(p => p.Usuario.Email == emails[1]);
    Check(professional.EspecialidadeId == especialidadeId, "Vinculo com especialidade persistido");
    var detail = await Request(HttpMethod.Get, "/api/profissional/" + professional.Id, null, HttpStatusCode.OK, "Detalhe profissional", patientToken);
    Check(!detail.TryGetProperty("cpf", out _) && !detail.TryGetProperty("dataNascimento", out _), "Resposta nao expoe CPF nem nascimento");
    Check(detail.GetProperty("especialidade").GetProperty("id").GetGuid() == especialidadeId, "Detalhe devolve especialidade escolhida");
    await Request(HttpMethod.Get, "/api/profissional?nome=" + prefix + "&ativo=true&pagina=1&tamanhoPagina=10", null, HttpStatusCode.OK, "Listagem com filtros", patientToken);
    await Request(HttpMethod.Get, "/api/profissional/" + Guid.NewGuid(), null, HttpStatusCode.NotFound, "Profissional inexistente", patientToken);

    await Request(HttpMethod.Get, "/api/agendamento", null, HttpStatusCode.Unauthorized, "Agenda exige autenticacao");
    var horario = DateTime.UtcNow.AddDays(10);
    await Request(HttpMethod.Post, "/api/agendamento", new { pacienteId = patient.Id, profissionalId = professional.Id, dataHora = DateTime.UtcNow.AddMinutes(-5), status = "Agendado" }, HttpStatusCode.BadRequest, "Agenda rejeita horario passado", patientToken);
    await Request(HttpMethod.Post, "/api/agendamento", new { pacienteId = patient.Id, profissionalId = professional.Id, dataHora = horario, status = "Invalido" }, HttpStatusCode.BadRequest, "Agenda rejeita status invalido", patientToken);
    await Request(HttpMethod.Post, "/api/agendamento", new { pacienteId = Guid.NewGuid(), profissionalId = professional.Id, dataHora = horario }, HttpStatusCode.NotFound, "Agenda rejeita paciente inexistente", patientToken);
    await Request(HttpMethod.Post, "/api/agendamento", new { pacienteId = patient.Id, profissionalId = Guid.NewGuid(), dataHora = horario }, HttpStatusCode.NotFound, "Agenda rejeita profissional inexistente", patientToken);
    var agendamento = await Request(HttpMethod.Post, "/api/agendamento", new { pacienteId = patient.Id, profissionalId = professional.Id, dataHora = horario, observacoes = "Teste de regressao" }, HttpStatusCode.Created, "Paciente cria agendamento", patientToken);
    var agendamentoId = agendamento.GetProperty("id").GetGuid();
    Check(agendamento.GetProperty("status").GetString() == "Agendado", "Status padrao do agendamento");
    await Request(HttpMethod.Get, "/api/agendamento/" + agendamentoId, null, HttpStatusCode.OK, "Consulta agendamento por id", patientToken);
    await Request(HttpMethod.Get, "/api/agendamento/" + Guid.NewGuid(), null, HttpStatusCode.NotFound, "Agendamento inexistente", patientToken);
    var dataFiltro = Uri.EscapeDataString(horario.ToString("yyyy-MM-dd"));
    var agendaFiltrada = await Request(HttpMethod.Get, "/api/agendamento?data=" + dataFiltro + "&profissionalId=" + professional.Id + "&status=Agendado", null, HttpStatusCode.OK, "Lista agenda com filtros", professionalToken);
    Check(agendaFiltrada.EnumerateArray().Any(a => a.GetProperty("id").GetGuid() == agendamentoId), "Filtro retorna agendamento criado");
    await Request(HttpMethod.Post, "/api/agendamento", new { pacienteId = patient.Id, profissionalId = professional.Id, dataHora = horario }, HttpStatusCode.Conflict, "Agenda rejeita conflito de horario", patientToken);

    var horarioCancelado = horario.AddHours(1);
    await Request(HttpMethod.Post, "/api/agendamento", new { pacienteId = patient.Id, profissionalId = professional.Id, dataHora = horarioCancelado, status = "Cancelado" }, HttpStatusCode.Created, "Registra horario cancelado", patientToken);
    await Request(HttpMethod.Post, "/api/agendamento", new { pacienteId = patient.Id, profissionalId = professional.Id, dataHora = horarioCancelado }, HttpStatusCode.Created, "Horario cancelado pode ser reagendado", patientToken);
}
finally
{
    // Exclui somente os registros criados por esta execução, por e-mails exatos.
    var patients = await db.Pacientes.Include(p => p.Endereco).Where(p => emails.Contains(p.Usuario.Email)).ToListAsync();
    var professionals = await db.Profissionais.Where(p => emails.Contains(p.Usuario.Email)).ToListAsync();
    var patientIds = patients.Select(p => p.Id).ToArray();
    var professionalIds = professionals.Select(p => p.Id).ToArray();
    db.Agendamentos.RemoveRange(await db.Agendamentos.Where(a => patientIds.Contains(a.PacienteId) || professionalIds.Contains(a.ProfissionalId)).ToListAsync());
    db.Enderecos.RemoveRange(patients.Select(p => p.Endereco));
    db.Pacientes.RemoveRange(patients);
    db.Profissionais.RemoveRange(professionals);
    if (especialidadeCriadaId.HasValue)
        db.Especialidades.RemoveRange(await db.Especialidades.Where(e => e.Id == especialidadeCriadaId.Value).ToListAsync());
    db.Usuarios.RemoveRange(await db.Usuarios.Where(u => emails.Contains(u.Email)).ToListAsync());
    await db.SaveChangesAsync();
    Console.WriteLine("Registros desta execucao removidos.");
}
Console.WriteLine($"{checks} verificacoes aprovadas.");
