using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AgendaiFisio.Constants;
using AgendaiFisio.Context;
using AgendaiFisio.DTOs.Agendamento;
using AgendaiFisio.Services.Paciente;
using AgendaiFisio.Validations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

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
var emails = new[] { prefix + "-pac@example.invalid", prefix + "-prof@example.invalid", prefix + "-pac2@example.invalid", prefix + "-prof2@example.invalid" };
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
string CreateExpiredToken()
{
    var jwtSettings = config.GetSection("JwtSettings");
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
        jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey ausente.")));
    var now = DateTime.UtcNow;
    var descriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(JwtClaimNames.Subject, Guid.NewGuid().ToString()),
            new Claim(JwtClaimNames.PerfilId, Guid.NewGuid().ToString()),
            new Claim(JwtClaimNames.Email, "expired@example.invalid"),
            new Claim(JwtClaimNames.Role, PerfilDeUsuario.Paciente)
        }),
        NotBefore = now.AddHours(-2),
        Expires = now.AddHours(-1),
        Issuer = jwtSettings["Issuer"],
        Audience = jwtSettings["Audience"],
        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
    };
    return new JwtSecurityTokenHandler().WriteToken(
        new JwtSecurityTokenHandler().CreateToken(descriptor));
}
object Patient(string cpf, bool address) => address
    ? new { nomeCompleto = prefix, cpf, dataNascimento = "1990-01-01", rua = "Rua de teste", numero = "10", cep = "01001000", complemento = "Sala 1", bairro = "Centro", cidade = "Sao Paulo", estado = "SP" }
    : new { nomeCompleto = prefix, cpf, dataNascimento = "1990-01-01", rua = "Rua de teste", numero = "11", cep = "01001000" };
object Professional(string cpf, string? nome = null) => new { nomeCompleto = nome ?? prefix, cpf, crefito = "T" + Guid.NewGuid().ToString("N")[..12], telefone = "11999999999", dataNascimento = "1990-01-01", bio = "Teste automatizado", ativo = true };
Check(await db.Database.CanConnectAsync(), "Banco conectado");
Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "Banco sem migracoes pendentes");
try
{
    var cpfValidator = new CpfValidoAttribute();
    foreach (var cpf in new[] { "11111111111", "123", "abcdefghijk", "52998224724" })
        Check(!cpfValidator.IsValid(cpf), "CPF invalido rejeitado: " + cpf);
    Check(cpfValidator.IsValid("52998224725") && cpfValidator.IsValid("529.982.247-25"), "CPF valido com e sem pontuacao");
    Check(await new PacienteService(db, NullLogger<PacienteService>.Instance).GetPacienteByIdAsync(Guid.NewGuid()) is null, "Paciente inexistente retorna null");
    var dataUtc = JsonSerializer.Deserialize<AgendamentoCreateDTO>(
        "{\"dataHora\":\"2026-10-04T17:00:00Z\"}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    var dataBrasilia = JsonSerializer.Deserialize<AgendamentoCreateDTO>(
        "{\"dataHora\":\"2026-10-04T14:00:00-03:00\"}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    Check(dataUtc!.DataHora == dataBrasilia!.DataHora && dataUtc.DataHora.Kind == DateTimeKind.Utc,
        "Offsets diferentes representam o mesmo instante UTC");
    try
    {
        JsonSerializer.Deserialize<AgendamentoCreateDTO>(
            "{\"dataHora\":\"2026-10-04T14:00:00\"}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Check(false, "Data sem offset rejeitada");
    }
    catch (JsonException)
    {
        Check(true, "Data sem offset rejeitada");
    }
    await Request(HttpMethod.Get, "/openapi/v1.json", null, HttpStatusCode.OK, "OpenAPI com pacote atualizado");
    await Request(HttpMethod.Get, "/api/profissional", null, HttpStatusCode.Unauthorized, "Listagem exige autenticacao");
    foreach (var role in new[] { "Admin", "Clinica", "Desconhecido", "" })
        await Request(HttpMethod.Post, "/api/auth/register", new { email = emails[2], senha = "Regression123!", tipoUsuario = role }, HttpStatusCode.BadRequest, "Cadastro rejeita papel " + role);
    for (var i = 0; i < 2; i++)
    {
        var result = await Request(HttpMethod.Post, "/api/auth/register", new { email = emails[i], senha = "Regression123!", tipoUsuario = i == 0 ? " paciente " : "pRoFiSsIoNaL" }, HttpStatusCode.OK, "Cadastro com normalizacao");
        Check(result.GetProperty("dados").GetProperty("tipoUsuario").GetString() == (i == 0 ? "Paciente" : "Profissional"), "Papel canonico persistido");
    }
    await Request(HttpMethod.Post, "/api/auth/register",
        new { email = emails[2], senha = "Regression123!", tipoUsuario = "Paciente" },
        HttpStatusCode.OK, "Cadastro de segundo paciente para isolar a agenda");
    await Request(HttpMethod.Post, "/api/auth/register",
        new { email = emails[3], senha = "Regression123!", tipoUsuario = "Profissional" },
        HttpStatusCode.OK, "Cadastro de segundo profissional para o historico");
    async Task<string> Login(int index)
    {
        var result = await Request(HttpMethod.Post, "/api/auth/login", new { email = emails[index], senha = "Regression123!" }, HttpStatusCode.OK, "Login");
        Check(result.GetProperty("accessToken").GetString() == result.GetProperty("token").GetString(),
            "Login mantem token compativel e devolve accessToken");
        Check(result.GetProperty("tokenType").GetString() == "Bearer" &&
            result.GetProperty("expiresAtUtc").GetDateTime() > DateTime.UtcNow,
            "Login informa tipo e validade do token");
        Check(result.GetProperty("usuario").GetProperty("perfilId").ValueKind == JsonValueKind.String,
            "Login localiza o perfil do usuario");
        return result.GetProperty("accessToken").GetString()!;
    }
    var patientToken = await Login(0);
    var professionalToken = await Login(1);
    var segundoPacienteToken = await Login(2);
    var segundoProfissionalToken = await Login(3);
    var adminLogin = await Request(HttpMethod.Post, "/api/auth/login", new { email = adminEmail, senha = adminSenha }, HttpStatusCode.OK, "Login da Clinica");
    var adminToken = adminLogin.GetProperty("accessToken").GetString()!;
    Check(adminLogin.GetProperty("usuario").GetProperty("perfilId").ValueKind == JsonValueKind.Null,
        "Clinica nao recebe perfil de paciente ou profissional");
    var pacienteMeAuth = await Request(HttpMethod.Get, "/api/auth/me", null, HttpStatusCode.OK,
        "Auth me devolve identidade autenticada", patientToken);
    Check(pacienteMeAuth.GetProperty("email").GetString() == emails[0] &&
        pacienteMeAuth.GetProperty("tipoUsuario").GetString() == PerfilDeUsuario.Paciente,
        "Auth me devolve email e papel corretos");
    await Request(HttpMethod.Get, "/api/auth/me", null, HttpStatusCode.Unauthorized,
        "Auth me exige login");
    await Request(HttpMethod.Get, "/api/auth/me", null, HttpStatusCode.Unauthorized,
        "Token invalido e rejeitado", "token.invalido.assinatura");
    await Request(HttpMethod.Get, "/api/auth/me", null, HttpStatusCode.Unauthorized,
        "Token expirado e rejeitado", CreateExpiredToken());
    await Request(HttpMethod.Post, "/api/auth/logout", null, HttpStatusCode.Unauthorized,
        "Logout exige login");
    var especialidades = await Request(HttpMethod.Get, "/api/especialidade", null, HttpStatusCode.OK, "Catalogo de especialidades publico");
    var especialidadeId = especialidades.EnumerateArray().First().GetProperty("id").GetGuid();
    await Request(HttpMethod.Get, "/api/especialidade/" + especialidadeId, null, HttpStatusCode.OK, "Consulta publica de especialidade");
    await Request(HttpMethod.Get, "/api/especialidade/" + Guid.NewGuid(), null, HttpStatusCode.NotFound, "Especialidade inexistente");
    await Request(HttpMethod.Post, "/api/especialidade", new { nome = "Especialidade sem autenticacao" }, HttpStatusCode.Unauthorized, "Cadastro de especialidade exige autenticacao");
    await Request(HttpMethod.Post, "/api/especialidade", new { nome = "Especialidade por profissional" }, HttpStatusCode.Forbidden, "Profissional nao cadastra especialidade", professionalToken);
    await Request(HttpMethod.Post, "/api/especialidade", new { nome = "Especialidade por paciente" }, HttpStatusCode.Forbidden, "Paciente nao acessa funcao da Clinica", patientToken);
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
    var segundoPaciente = await db.Pacientes.AsNoTracking().SingleAsync(p => p.Usuario.Email == emails[2]);
    Check(pacienteMeAuth.GetProperty("perfilId").GetGuid() == patient.Id,
        "Auth me devolve o perfil correto");
    var pacienteMe = await Request(HttpMethod.Get, "/api/paciente/me", null, HttpStatusCode.OK,
        "Paciente consulta o proprio perfil sem informar id", patientToken);
    Check(pacienteMe.GetProperty("id").GetGuid() == patient.Id &&
        pacienteMe.GetProperty("usuarioId").GetGuid() == patient.UsuarioId &&
        pacienteMe.GetProperty("email").GetString() == emails[0],
        "Paciente me devolve conta e perfil corretos");
    await Request(HttpMethod.Get, "/api/paciente/me", null, HttpStatusCode.Forbidden,
        "Profissional nao usa perfil me de paciente", professionalToken);
    Check(patient.Endereco.Complemento == "Sala 1" && patient.Endereco.Bairro == "Centro" && patient.Endereco.Cidade == "Sao Paulo" && patient.Endereco.Estado == "SP" && patient.Endereco.Numero == "11", "Endereco preservado no banco");
    await Request(HttpMethod.Put, "/api/profissional/completar-perfil", Professional("52998224725"), HttpStatusCode.OK, "Profissional atualiza perfil", professionalToken);
    await Request(HttpMethod.Put, "/api/profissional/completar-perfil", Professional("52998224725", prefix + " Segundo"), HttpStatusCode.OK, "Segundo profissional atualiza perfil", segundoProfissionalToken);
    await Request(HttpMethod.Put, "/api/profissional/especialidade", new { especialidadeId = (Guid?)null }, HttpStatusCode.BadRequest, "Especialidade nula rejeitada", professionalToken);
    await Request(HttpMethod.Put, "/api/profissional/especialidade", new { especialidadeId = Guid.NewGuid() }, HttpStatusCode.BadRequest, "Especialidade inexistente rejeitada", professionalToken);
    await Request(HttpMethod.Put, "/api/profissional/especialidade", new { especialidadeId }, HttpStatusCode.OK, "Profissional escolhe especialidade apos cadastro", professionalToken);
    var professional = await db.Profissionais.AsNoTracking().SingleAsync(p => p.Usuario.Email == emails[1]);
    var segundoProfissional = await db.Profissionais.AsNoTracking().SingleAsync(p => p.Usuario.Email == emails[3]);
    Check(professional.EspecialidadeId == especialidadeId, "Vinculo com especialidade persistido");
    var detail = await Request(HttpMethod.Get, "/api/profissional/" + professional.Id, null, HttpStatusCode.OK, "Detalhe profissional", patientToken);
    Check(!detail.TryGetProperty("cpf", out _) && !detail.TryGetProperty("dataNascimento", out _), "Resposta nao expoe CPF nem nascimento");
    Check(detail.GetProperty("especialidade").GetProperty("id").GetGuid() == especialidadeId, "Detalhe devolve especialidade escolhida");
    await Request(HttpMethod.Get, "/api/profissional?nome=" + prefix + "&ativo=true&pagina=1&tamanhoPagina=10", null, HttpStatusCode.OK, "Listagem com filtros", patientToken);
    await Request(HttpMethod.Get, "/api/profissional/" + Guid.NewGuid(), null, HttpStatusCode.NotFound, "Profissional inexistente", patientToken);

    var rotaHistorico = "/api/paciente/" + patient.Id + "/historico-consultas";
    const string rotaMeuHistorico = "/api/paciente/me/historico-consultas";
    await Request(HttpMethod.Get, rotaHistorico, null, HttpStatusCode.Unauthorized, "Historico exige login");
    await Request(HttpMethod.Get, rotaHistorico, null, HttpStatusCode.Forbidden, "Clinica nao acessa historico clinico", adminToken);
    var semVinculo = await Request(HttpMethod.Get, rotaHistorico, null, HttpStatusCode.NotFound,
        "Profissional sem agendamento nao acessa historico", professionalToken);
    await Request(HttpMethod.Get, rotaHistorico, null, HttpStatusCode.Forbidden,
        "Paciente nao usa rota explicita de outro usuario", segundoPacienteToken);
    await Request(HttpMethod.Get, rotaHistorico, null, HttpStatusCode.NotFound,
        "Segundo profissional sem vinculo nao acessa historico", segundoProfissionalToken);
    var inexistente = await Request(HttpMethod.Get, "/api/paciente/" + Guid.NewGuid() + "/historico-consultas",
        null, HttpStatusCode.NotFound, "Historico de paciente inexistente", professionalToken);
    Check(semVinculo.GetRawText() == inexistente.GetRawText(),
        "Paciente inexistente e inacessivel recebem a mesma resposta");
    var historicoInicial = await Request(HttpMethod.Get, rotaMeuHistorico, null, HttpStatusCode.OK, "Paciente consulta o proprio historico", patientToken);
    Check(historicoInicial.GetProperty("itens").GetArrayLength() == 0 &&
        historicoInicial.GetProperty("totalRegistros").GetInt32() == 0, "Historico vazio retorna pagina vazia");

    await Request(HttpMethod.Get, "/api/agendamento", null, HttpStatusCode.Unauthorized, "Agenda exige autenticacao");
    var horario = DateTime.UtcNow.AddDays(10);
    await Request(HttpMethod.Post, "/api/agendamento", new { profissionalId = professional.Id, dataHora = DateTime.UtcNow.AddMinutes(-5), status = "Agendado" }, HttpStatusCode.BadRequest, "Agenda rejeita horario passado", patientToken);
    await Request(HttpMethod.Post, "/api/agendamento", new { profissionalId = professional.Id, dataHora = horario, status = "Invalido" }, HttpStatusCode.BadRequest, "Agenda rejeita status invalido", patientToken);
    await Request(HttpMethod.Post, "/api/agendamento", new { profissionalId = professional.Id, dataHora = horario.ToString("yyyy-MM-ddTHH:mm:ss") }, HttpStatusCode.BadRequest, "Agenda rejeita data sem offset", patientToken);
    await Request(HttpMethod.Post, "/api/agendamento", new { profissionalId = professional.Id, dataHora = horario }, HttpStatusCode.Forbidden, "Profissional nao cria vinculo arbitrario", professionalToken);
    await Request(HttpMethod.Post, "/api/agendamento", new { profissionalId = professional.Id, dataHora = horario }, HttpStatusCode.Forbidden, "Clinica usa somente fluxo administrativo", adminToken);
    await Request(HttpMethod.Get, rotaHistorico, null, HttpStatusCode.NotFound, "Tentativa de vinculo nao libera historico", professionalToken);
    await Request(HttpMethod.Post, "/api/agendamento/administrativo", new { pacienteId = Guid.NewGuid(), profissionalId = professional.Id, dataHora = horario }, HttpStatusCode.NotFound, "Clinica recebe 404 para paciente inexistente", adminToken);
    await Request(HttpMethod.Post, "/api/agendamento", new { profissionalId = Guid.NewGuid(), dataHora = horario }, HttpStatusCode.NotFound, "Agenda rejeita profissional inexistente", patientToken);
    var agendamento = await Request(HttpMethod.Post, "/api/agendamento", new { profissionalId = professional.Id, dataHora = horario, observacoes = "Teste de regressao" }, HttpStatusCode.Created, "Paciente cria agendamento sem enviar o proprio id", patientToken);
    var agendamentoId = agendamento.GetProperty("id").GetGuid();
    Check(agendamento.GetProperty("status").GetString() == "Agendado" &&
        agendamento.GetProperty("dataHora").GetString()!.EndsWith("Z"), "Status padrao e resposta UTC do agendamento");
    var horarioBrasilia = new DateTimeOffset(horario).ToOffset(TimeSpan.FromHours(-3));
    var agendamentoComOffset = await Request(HttpMethod.Post, "/api/agendamento",
        new { profissionalId = segundoProfissional.Id, dataHora = horarioBrasilia },
        HttpStatusCode.Created, "Paciente agenda com offset de Brasilia", patientToken);
    Check(DateTime.Parse(agendamentoComOffset.GetProperty("dataHora").GetString()!).ToUniversalTime() == horario,
        "Offset de Brasilia e normalizado para o mesmo instante UTC");
    var dataPersistida = await db.Agendamentos.AsNoTracking()
        .Where(a => a.Id == agendamentoComOffset.GetProperty("id").GetGuid())
        .Select(a => a.DataHora).SingleAsync();
    Check(dataPersistida == horario, "Banco armazena horario normalizado em UTC");

    var agendamentoDaClinica = await Request(HttpMethod.Post, "/api/agendamento/administrativo",
        new { pacienteId = segundoPaciente.Id, profissionalId = professional.Id, dataHora = horario.AddHours(2) },
        HttpStatusCode.Created, "Clinica agenda para outro paciente", adminToken);
    var agendamentoDaClinicaId = agendamentoDaClinica.GetProperty("id").GetGuid();
    var agendaPaciente = await Request(HttpMethod.Get, "/api/agendamento", null, HttpStatusCode.OK,
        "Paciente lista sua agenda", patientToken);
    Check(agendaPaciente.EnumerateArray().Any(a => a.GetProperty("id").GetGuid() == agendamentoId) &&
        !agendaPaciente.EnumerateArray().Any(a => a.GetProperty("id").GetGuid() == agendamentoDaClinicaId),
        "Agenda do paciente nao inclui outro paciente");
    var agendaSegundoProfissional = await Request(HttpMethod.Get, "/api/agendamento", null, HttpStatusCode.OK,
        "Profissional lista sua agenda", segundoProfissionalToken);
    Check(agendaSegundoProfissional.EnumerateArray().All(a => a.GetProperty("profissionalId").GetGuid() == segundoProfissional.Id),
        "Profissional nao lista agendamentos alheios");
    await Request(HttpMethod.Get, "/api/agendamento/" + agendamentoDaClinicaId, null,
        HttpStatusCode.NotFound, "Paciente nao consulta agendamento de outro paciente", patientToken);
    await Request(HttpMethod.Get, "/api/agendamento/" + agendamentoId, null,
        HttpStatusCode.NotFound, "Profissional nao consulta agendamento de outro profissional", segundoProfissionalToken);
    await Request(HttpMethod.Get, "/api/agendamento/" + agendamentoDaClinicaId, null,
        HttpStatusCode.OK, "Clinica consulta agendamento administrativo", adminToken);
    var historicoDaClinica = await Request(HttpMethod.Get,
        "/api/paciente/" + segundoPaciente.Id + "/historico-consultas", null, HttpStatusCode.OK,
        "Agendamento da clinica libera historico ao profissional", professionalToken);
    Check(historicoDaClinica.GetProperty("itens").GetArrayLength() == 0,
        "Paciente com apenas agendamento futuro tem historico vazio");
    var historicoAntesDaConsulta = await Request(HttpMethod.Get, rotaHistorico, null, HttpStatusCode.OK,
        "Profissional com agendamento consulta historico", professionalToken);
    Check(historicoAntesDaConsulta.GetProperty("itens").GetArrayLength() == 0,
        "Agendamento futuro nao aparece no historico");

    var consultaAntiga = new AgendaiFisio.Entities.Agendamento
    {
        PacienteId = patient.Id, ProfissionalId = professional.Id,
        DataHora = DateTime.UtcNow.AddDays(-3), Status = "Concluido"
    };
    var consultaRecente = new AgendaiFisio.Entities.Agendamento
    {
        PacienteId = patient.Id, ProfissionalId = segundoProfissional.Id,
        DataHora = DateTime.UtcNow.AddDays(-1), Status = "Cancelado"
    };
    var consultasAdicionais = Enumerable.Range(0, 53)
        .Select(i => new AgendaiFisio.Entities.Agendamento
        {
            PacienteId = patient.Id, ProfissionalId = professional.Id,
            DataHora = DateTime.UtcNow.AddDays(-4 - i), Status = "Concluido"
        }).ToList();
    db.Agendamentos.AddRange(consultasAdicionais);
    db.Agendamentos.AddRange(consultaAntiga, consultaRecente);
    await db.SaveChangesAsync();

    var historico = await Request(HttpMethod.Get, rotaMeuHistorico + "?pagina=1&tamanhoPagina=2", null,
        HttpStatusCode.OK, "Paciente consulta primeira pagina do historico", patientToken);
    var primeirosItens = historico.GetProperty("itens");
    Check(primeirosItens.GetArrayLength() == 2 && historico.GetProperty("totalRegistros").GetInt32() == 55 &&
        historico.GetProperty("paginaAtual").GetInt32() == 1 &&
        historico.GetProperty("totalPaginas").GetInt32() == 28,
        "Historico informa total e navegacao por paginas");
    Check(primeirosItens[0].GetProperty("agendamentoId").GetGuid() == consultaRecente.Id &&
        primeirosItens[0].GetProperty("status").GetString() == "Cancelado" &&
        primeirosItens[1].GetProperty("agendamentoId").GetGuid() == consultaAntiga.Id &&
        primeirosItens[1].GetProperty("status").GetString() == "Concluido",
        "Historico ordenado do mais recente ao mais antigo");
    Check(primeirosItens[0].GetProperty("profissionalId").GetGuid() == segundoProfissional.Id &&
        primeirosItens[1].GetProperty("profissionalId").GetGuid() == professional.Id &&
        primeirosItens[0].GetProperty("dataHora").GetString()!.EndsWith("Z"),
        "Historico inclui profissionais diferentes e data UTC explicita");
    var segundaPagina = await Request(HttpMethod.Get, rotaMeuHistorico + "?pagina=2&tamanhoPagina=2", null,
        HttpStatusCode.OK, "Paciente consulta segunda pagina", patientToken);
    Check(segundaPagina.GetProperty("itens").GetArrayLength() == 2 &&
        segundaPagina.GetProperty("itens")[0].GetProperty("agendamentoId").GetGuid() !=
        primeirosItens[1].GetProperty("agendamentoId").GetGuid(),
        "Paginas consecutivas mantem ordem estavel sem repeticao");
    var paginaMaxima = await Request(HttpMethod.Get, rotaMeuHistorico + "?pagina=1&tamanhoPagina=500", null,
        HttpStatusCode.OK, "Historico limita tamanho por chamada", patientToken);
    Check(paginaMaxima.GetProperty("itens").GetArrayLength() == 50 &&
        paginaMaxima.GetProperty("tamanhoPagina").GetInt32() == 50 &&
        paginaMaxima.GetProperty("totalPaginas").GetInt32() == 2,
        "Limite maximo de 50 itens respeitado");
    var ultimaPagina = await Request(HttpMethod.Get, rotaMeuHistorico + "?pagina=2&tamanhoPagina=50", null,
        HttpStatusCode.OK, "Paciente consulta ultima pagina", patientToken);
    Check(ultimaPagina.GetProperty("itens").GetArrayLength() == 5,
        "Todos os registros podem ser percorridos");
    var paginaNormalizada = await Request(HttpMethod.Get, rotaMeuHistorico + "?pagina=0&tamanhoPagina=0", null,
        HttpStatusCode.OK, "Parametros de pagina invalidos normalizados", patientToken);
    Check(paginaNormalizada.GetProperty("paginaAtual").GetInt32() == 1 &&
        paginaNormalizada.GetProperty("tamanhoPagina").GetInt32() == 10,
        "Pagina e tamanho invalidos usam os padroes");
    var historicoDoProfissional = await Request(HttpMethod.Get, rotaHistorico, null, HttpStatusCode.OK,
        "Profissional consulta historico do paciente agendado", professionalToken);
    Check(historicoDoProfissional.GetProperty("totalRegistros").GetInt32() == 55,
        "Profissional ve consultas de outros profissionais no historico");
    await Request(HttpMethod.Get, "/api/agendamento/" + agendamentoId, null, HttpStatusCode.OK, "Consulta agendamento por id", patientToken);
    await Request(HttpMethod.Get, "/api/agendamento/" + Guid.NewGuid(), null, HttpStatusCode.NotFound, "Agendamento inexistente", patientToken);
    var dataFiltro = Uri.EscapeDataString(horario.ToString("yyyy-MM-dd"));
    var agendaFiltrada = await Request(HttpMethod.Get, "/api/agendamento?data=" + dataFiltro + "&profissionalId=" + professional.Id + "&status=Agendado", null, HttpStatusCode.OK, "Lista agenda com filtros", professionalToken);
    Check(agendaFiltrada.EnumerateArray().Any(a => a.GetProperty("id").GetGuid() == agendamentoId), "Filtro retorna agendamento criado");
    await Request(HttpMethod.Post, "/api/agendamento", new { profissionalId = professional.Id, dataHora = horarioBrasilia }, HttpStatusCode.Conflict, "Agenda rejeita conflito mesmo com offset distinto", patientToken);

    var horarioCancelado = horario.AddHours(1);
    await Request(HttpMethod.Post, "/api/agendamento", new { profissionalId = professional.Id, dataHora = horarioCancelado, status = "Cancelado" }, HttpStatusCode.Created, "Registra horario cancelado", patientToken);
    await Request(HttpMethod.Post, "/api/agendamento", new { profissionalId = professional.Id, dataHora = horarioCancelado }, HttpStatusCode.Created, "Horario cancelado pode ser reagendado", patientToken);
    await Request(HttpMethod.Post, "/api/auth/logout", null, HttpStatusCode.NoContent,
        "Logout autenticado encerra a sessao do cliente", patientToken);
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
