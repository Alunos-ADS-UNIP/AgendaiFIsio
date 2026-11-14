using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using AgendaiFisio.Validations;
using Microsoft.EntityFrameworkCore;

const string confirmacaoEsperada = "APAGAR_E_RECRIAR_DADOS";
const string senhaDemonstracao = "AgendaFisio@2026";

var perfis = CriarPerfis();
var diretorioFotos = Path.Combine(AppContext.BaseDirectory, "Assets");
ValidarPerfis(perfis, diretorioFotos);

if (args.Contains("--validate-only", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine($"Carga válida: {perfis.Count} terapeutas, " +
                      $"{perfis.Select(p => p.EspecialidadeId).Distinct().Count()} especialidades e 6 fotos.");
    return;
}

if (!string.Equals(
        Environment.GetEnvironmentVariable("AGENDAIFISIO_SEED_CONFIRM"),
        confirmacaoEsperada,
        StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        $"Operação bloqueada. Configure AGENDAIFISIO_SEED_CONFIRM={confirmacaoEsperada} para confirmar a limpeza.");
}

var arquivoConexaoLocal = Path.Combine(
    Directory.GetCurrentDirectory(), "Tools", "DemoDataSeeder", "connection-string.local.txt");
var connectionString =
    Environment.GetEnvironmentVariable("AGENDAIFISIO_SEED_CONNECTION_STRING") ??
    Environment.GetEnvironmentVariable("ConnectionStrings__Padrao") ??
    (File.Exists(arquivoConexaoLocal)
        ? (await File.ReadAllTextAsync(arquivoConexaoLocal)).Trim()
        : null);
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure AGENDAIFISIO_SEED_CONNECTION_STRING com a conexão do SQL Server de destino.");
}

var apiUrl = Environment.GetEnvironmentVariable("AGENDAIFISIO_SEED_API_URL") ??
             "http://agendaifisio.runasp.net";
if (!Uri.TryCreate(apiUrl?.TrimEnd('/') + "/", UriKind.Absolute, out var apiBaseUrl) ||
    apiBaseUrl.Scheme is not ("http" or "https"))
{
    throw new InvalidOperationException(
        "Configure AGENDAIFISIO_SEED_API_URL com a URL pública da API de destino.");
}

using var http = new HttpClient { BaseAddress = apiBaseUrl, Timeout = TimeSpan.FromSeconds(60) };
using (var health = await http.GetAsync("health"))
{
    if (!health.IsSuccessStatusCode)
        throw new InvalidOperationException(
            $"A API de destino não está saudável (HTTP {(int)health.StatusCode}).");
}

var options = new DbContextOptionsBuilder<AgendaiFisioDbContext>()
    // A carga inteira usa uma transação explícita; por isso não ativa a estratégia de retry,
    // que exige que a própria estratégia controle o início e o fim da transação.
    .UseSqlServer(connectionString)
    .Options;
await using var db = new AgendaiFisioDbContext(options);

if (!await db.Database.CanConnectAsync())
    throw new InvalidOperationException("Não foi possível conectar ao SQL Server informado.");

var migrationsPendentes = await db.Database.GetPendingMigrationsAsync();
if (migrationsPendentes.Any())
{
    throw new InvalidOperationException(
        "O banco possui migrations pendentes: " + string.Join(", ", migrationsPendentes));
}

var especialidadesExistentes = await db.Especialidades.AsNoTracking()
    .OrderBy(e => e.Id)
    .Select(e => e.Id)
    .ToListAsync();
if (especialidadesExistentes.Count != EspecialidadeCatalogoSeed.Itens.Count ||
    perfis.Any(p => !especialidadesExistentes.Contains(p.EspecialidadeId)))
{
    throw new InvalidOperationException(
        "O catálogo de especialidades do banco não corresponde ao catálogo esperado pela carga.");
}

await ValidarUploadAntesDaLimpezaAsync(http, db, diretorioFotos, senhaDemonstracao);

await using (var transaction = await db.Database.BeginTransactionAsync())
{
    try
    {
        Console.WriteLine("Removendo dados operacionais e contas existentes...");
        await db.Agendamentos.ExecuteDeleteAsync();
        await db.BloqueiosAgenda.ExecuteDeleteAsync();
        await db.Pacientes.ExecuteDeleteAsync();
        await db.Profissionais.ExecuteDeleteAsync();
        await db.Usuarios.ExecuteDeleteAsync();
        await db.Enderecos.ExecuteDeleteAsync();

        var senhaHash = BCrypt.Net.BCrypt.HashPassword(senhaDemonstracao);
        foreach (var perfil in perfis)
        {
            var usuario = new Usuario
            {
                Id = perfil.UsuarioId,
                Email = perfil.Email,
                SenhaHash = senhaHash,
                TipoUsuario = "Profissional",
                AceiteLgpd = true,
                AceiteLgpdEm = DateTime.UtcNow
            };
            var endereco = new Endereco
            {
                Id = perfil.EnderecoId,
                Rua = perfil.Rua,
                Numero = perfil.Numero,
                Complemento = perfil.Complemento,
                Cep = perfil.Cep,
                Bairro = perfil.Bairro,
                Cidade = perfil.Cidade,
                Estado = perfil.Estado
            };
            var profissional = new Profissional
            {
                Id = perfil.ProfissionalId,
                NomeCompleto = perfil.Nome,
                Cpf = perfil.Cpf,
                Crefito = perfil.Crefito,
                Telefone = perfil.Telefone,
                DataNascimento = perfil.DataNascimento,
                Bio = perfil.Bio,
                DataCadastro = DateTime.UtcNow,
                Ativo = true,
                EnderecoId = endereco.Id,
                Endereco = endereco,
                EspecialidadeId = perfil.EspecialidadeId,
                UsuarioId = usuario.Id,
                Usuario = usuario
            };
            db.Profissionais.Add(profissional);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

Console.WriteLine("Enviando as fotos pela API autenticada...");
for (var indice = 0; indice < perfis.Count; indice++)
{
    var perfil = perfis[indice];
    var token = await ObterTokenAsync(http, perfil.Email, senhaDemonstracao);
    var foto = Path.Combine(diretorioFotos, $"perfil-{perfil.FotoNumero:00}.jpg");
    await EnviarFotoAsync(http, token, foto, perfil.Email);
    Console.WriteLine($"Foto {indice + 1:00}/{perfis.Count}: {perfil.Email}");
}

var totalUsuarios = await db.Usuarios.CountAsync();
var totalProfissionais = await db.Profissionais.CountAsync();
var totalComFoto = await db.Profissionais.CountAsync(p => p.FotoPerfilChave != null);
var totalEnderecos = await db.Enderecos.CountAsync();
if (totalUsuarios != 48 || totalProfissionais != 48 || totalComFoto != 48 || totalEnderecos != 48)
{
    throw new InvalidOperationException(
        $"Validação final inesperada: usuários={totalUsuarios}, profissionais={totalProfissionais}, " +
        $"fotos={totalComFoto}, endereços={totalEnderecos}.");
}

var caminhoCredenciais = Environment.GetEnvironmentVariable("DEMO_CREDENTIALS_OUTPUT") ??
                         Path.Combine(Directory.GetCurrentDirectory(), "output", "credenciais-terapeutas.csv");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(caminhoCredenciais))!);
var csv = new StringBuilder("Email;Senha;Nome;Especialidade\n");
foreach (var perfil in perfis.OrderBy(p => p.EspecialidadeNome).ThenBy(p => p.Nome))
{
    csv.Append(perfil.Email).Append(';')
        .Append(senhaDemonstracao).Append(';')
        .Append(perfil.Nome).Append(';')
        .Append(perfil.EspecialidadeNome).AppendLine();
}
await File.WriteAllTextAsync(caminhoCredenciais, csv.ToString(), new UTF8Encoding(true));

Console.WriteLine("Carga concluída com sucesso.");
Console.WriteLine("48 usuários profissionais, 48 endereços e 48 fotos foram inseridos.");
Console.WriteLine($"Credenciais salvas em: {Path.GetFullPath(caminhoCredenciais)}");

static async Task ValidarUploadAntesDaLimpezaAsync(
    HttpClient http,
    AgendaiFisioDbContext db,
    string diretorioFotos,
    string senha)
{
    var email = $"seed-check-{Guid.NewGuid():N}@agendaifisio.invalid";
    Console.WriteLine("Validando cadastro, login, JWT e S3 antes da limpeza...");
    try
    {
        using var cadastro = await http.PostAsJsonAsync("api/auth/register", new
        {
            email,
            senha,
            tipoUsuario = "Profissional",
            aceiteLgpd = true
        });
        var cadastroBody = await cadastro.Content.ReadAsStringAsync();
        if (!cadastro.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Falha no cadastro de validação (HTTP {(int)cadastro.StatusCode}): {cadastroBody}");
        }

        var token = await ObterTokenAsync(http, email, senha);
        await EnviarFotoAsync(
            http,
            token,
            Path.Combine(diretorioFotos, "perfil-01.jpg"),
            email);

        using var removerRequest = new HttpRequestMessage(HttpMethod.Delete, "api/profissional/me/foto");
        removerRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var removerResponse = await http.SendAsync(removerRequest);
        var removerBody = await removerResponse.Content.ReadAsStringAsync();
        if (!removerResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Falha ao remover a foto de validação " +
                $"(HTTP {(int)removerResponse.StatusCode}): {removerBody}");
        }
    }
    finally
    {
        db.ChangeTracker.Clear();
        var profissional = await db.Profissionais
            .Include(p => p.Endereco)
            .Include(p => p.Usuario)
            .SingleOrDefaultAsync(p => p.Usuario.Email == email);
        if (profissional is not null)
        {
            var endereco = profissional.Endereco;
            var usuario = profissional.Usuario;
            db.Profissionais.Remove(profissional);
            if (endereco is not null) db.Enderecos.Remove(endereco);
            db.Usuarios.Remove(usuario);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }
    }
    Console.WriteLine("Validação de upload aprovada; a limpeza pode prosseguir.");
}

static async Task<string> ObterTokenAsync(
    HttpClient http,
    string email,
    string senha)
{
    using var response = await http.PostAsJsonAsync("api/auth/login", new { email, senha });
    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"Falha no login de {email} (HTTP {(int)response.StatusCode}): {body}");
    }

    using var json = JsonDocument.Parse(body);
    return json.RootElement.GetProperty("accessToken").GetString()
           ?? throw new InvalidOperationException("A API não devolveu o accessToken esperado.");
}

static async Task EnviarFotoAsync(
    HttpClient http,
    string token,
    string caminhoFoto,
    string email)
{
    await using var arquivo = File.OpenRead(caminhoFoto);
    using var arquivoHttp = new StreamContent(arquivo);
    arquivoHttp.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
    using var multipart = new MultipartFormDataContent();
    multipart.Add(arquivoHttp, "arquivo", Path.GetFileName(caminhoFoto));
    using var request = new HttpRequestMessage(HttpMethod.Put, "api/profissional/me/foto")
    {
        Content = multipart
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    using var response = await http.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"Falha no upload da foto de {email} (HTTP {(int)response.StatusCode}): {body}");
    }
}

static List<PerfilDemo> CriarPerfis()
{
    var nomes = new[]
    {
        "Ana Souza", "Bruno Almeida", "Carla Mendes", "Diego Ribeiro", "Elisa Martins", "Felipe Costa",
        "Gabriela Rocha", "Henrique Lima", "Isabela Nunes", "João Carvalho", "Karina Ferreira", "Lucas Barbosa",
        "Mariana Oliveira", "Nicolas Azevedo", "Olivia Santos", "Paulo Moreira", "Renata Castro", "Samuel Freitas",
        "Tatiana Lopes", "Vinicius Moraes", "Aline Cardoso", "Bernardo Teixeira", "Camila Farias", "Daniel Peixoto",
        "Érica Gonçalves", "Fernando Pires", "Giovana Dias", "Heitor Campos", "Ingrid Araújo", "Júlio Rezende",
        "Larissa Monteiro", "Marcos Tavares", "Natália Vieira", "Otávio Cunha", "Patrícia Sales", "Rafael Dantas",
        "Sabrina Correia", "Thiago Borges", "Vanessa Leal", "Wesley Moura", "Yasmin Brito", "Arthur Neves",
        "Beatriz Coelho", "Caio Fernandes", "Débora Pinto", "Eduardo Macedo", "Flávia Reis", "Gustavo Prado"
    };
    var ruas = new[]
    {
        ("Avenida Nove de Julho", "1700", "Centro", "14015000"),
        ("Avenida Presidente Vargas", "950", "Jardim América", "14020260"),
        ("Rua Bernardino de Campos", "1250", "Higienópolis", "14015130"),
        ("Avenida Portugal", "780", "Jardim São Luiz", "14020280"),
        ("Rua São José", "1020", "Centro", "14010000"),
        ("Avenida Independência", "2100", "Alto da Boa Vista", "14025500")
    };

    var perfis = new List<PerfilDemo>();
    var indice = 0;
    foreach (var especialidade in EspecialidadeCatalogoSeed.Itens)
    {
        for (var vaga = 0; vaga < 3; vaga++)
        {
            var numero = indice + 1;
            var endereco = ruas[indice % ruas.Length];
            perfis.Add(new PerfilDemo
            {
                UsuarioId = Guid.Parse($"10000000-0000-4000-8000-{numero:000000000000}"),
                ProfissionalId = Guid.Parse($"20000000-0000-4000-8000-{numero:000000000000}"),
                EnderecoId = Guid.Parse($"30000000-0000-4000-8000-{numero:000000000000}"),
                Nome = nomes[indice],
                Email = $"terapeuta{numero:00}@agendaifisio.demo",
                Cpf = GerarCpf(numero),
                Crefito = $"CREFITO-3/{120000 + numero}-F",
                Telefone = $"1699{numero:0000000}",
                DataNascimento = new DateTime(1978 + indice % 20, 1 + indice % 12, 1 + indice % 27),
                Bio = $"Fisioterapeuta com atuação em {especialidade.Nome}, atendimento humanizado e plano terapêutico individualizado.",
                EspecialidadeId = especialidade.Id,
                EspecialidadeNome = especialidade.Nome,
                Rua = endereco.Item1,
                Numero = endereco.Item2,
                Complemento = $"Sala {101 + indice}",
                Cep = endereco.Item4,
                Bairro = endereco.Item3,
                Cidade = "Ribeirão Preto",
                Estado = "SP",
                FotoNumero = indice % 6 + 1
            });
            indice++;
        }
    }
    return perfis;
}

static void ValidarPerfis(IReadOnlyCollection<PerfilDemo> perfis, string diretorioFotos)
{
    if (perfis.Count != 48)
        throw new InvalidOperationException("A carga deve conter exatamente 48 profissionais.");
    if (perfis.Select(p => p.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count() != perfis.Count)
        throw new InvalidOperationException("A carga contém e-mails duplicados.");
    if (perfis.Select(p => p.Crefito).Distinct(StringComparer.OrdinalIgnoreCase).Count() != perfis.Count)
        throw new InvalidOperationException("A carga contém CREFITOs duplicados.");
    var cpfValidator = new CpfValidoAttribute();
    if (perfis.Any(p => !cpfValidator.IsValid(p.Cpf)))
        throw new InvalidOperationException("A carga contém CPF inválido.");
    if (perfis.GroupBy(p => p.EspecialidadeId).Any(g => g.Count() != 3))
        throw new InvalidOperationException("Cada especialidade deve possuir três profissionais.");
    for (var foto = 1; foto <= 6; foto++)
    {
        var caminho = Path.Combine(diretorioFotos, $"perfil-{foto:00}.jpg");
        if (!File.Exists(caminho))
            throw new FileNotFoundException("Foto de demonstração não encontrada.", caminho);
    }
}

static string GerarCpf(int numero)
{
    var baseCpf = (100_000_000 + numero).ToString("000000000", CultureInfo.InvariantCulture);
    var digitos = baseCpf.Select(c => c - '0').ToList();
    var primeiro = CalcularDigitoCpf(digitos, 10);
    digitos.Add(primeiro);
    var segundo = CalcularDigitoCpf(digitos, 11);
    return baseCpf + primeiro + segundo;
}

static int CalcularDigitoCpf(IReadOnlyList<int> digitos, int pesoInicial)
{
    var soma = 0;
    for (var i = 0; i < digitos.Count; i++)
        soma += digitos[i] * (pesoInicial - i);
    var resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
}

file sealed class PerfilDemo
{
    public Guid UsuarioId { get; init; }
    public Guid ProfissionalId { get; init; }
    public Guid EnderecoId { get; init; }
    public required string Nome { get; init; }
    public required string Email { get; init; }
    public required string Cpf { get; init; }
    public required string Crefito { get; init; }
    public required string Telefone { get; init; }
    public DateTime DataNascimento { get; init; }
    public required string Bio { get; init; }
    public Guid EspecialidadeId { get; init; }
    public required string EspecialidadeNome { get; init; }
    public required string Rua { get; init; }
    public required string Numero { get; init; }
    public required string Complemento { get; init; }
    public required string Cep { get; init; }
    public required string Bairro { get; init; }
    public required string Cidade { get; init; }
    public required string Estado { get; init; }
    public int FotoNumero { get; init; }
}
