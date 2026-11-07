using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using AgendaiFisio.Constants;
using AgendaiFisio.Context;
using AgendaiFisio.Services.Auth;
using AgendaiFisio.Services.Especialidade;
using AgendaiFisio.Services.Paciente;
using AgendaiFisio.Services.Profissional;
using AgendaiFisio.Services.Agendamento;
using AgendaiFisio.Services.BloqueioAgenda;
using AgendaiFisio.OpenApi;
using AgendaiFisio.Repositorio.Interfaces;
using AgendaiFisio.Repositorio.Repositorios;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Padrao");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Padrao' não encontrada. Configure ConnectionStrings__Padrao.");
}

builder.Services.AddDbContext<AgendaiFisioDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure()));

// Repositórios: somente esta camada conhece o DbContext e o SQL Server.
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IPacienteRepositorio, PacienteRepositorio>();
builder.Services.AddScoped<IProfissionalRepositorio, ProfissionalRepositorio>();
builder.Services.AddScoped<IEspecialidadeRepositorio, EspecialidadeRepositorio>();
builder.Services.AddScoped<IAgendamentoRepositorio, AgendamentoRepositorio>();
builder.Services.AddScoped<IBloqueioAgendaRepositorio, BloqueioAgendaRepositorio>();
builder.Services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

// Negócio: os controllers recebem somente serviços desta camada.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtualService, UsuarioAtualService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IProfissionalService, ProfissionalService>();
builder.Services.AddScoped<IEspecialidadeService, EspecialidadeService>();
builder.Services.AddScoped<IAgendamentoService, AgendamentoService>();
builder.Services.AddScoped<IBloqueioAgendaService, BloqueioAgendaService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey");
if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JwtSettings__SecretKey deve ser configurada com pelo menos 32 caracteres.");
}

// Define como os tokens de acesso serão conferidos.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey!)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidateAudience = true,
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtClaimNames.Subject,
        RoleClaimType = JwtClaimNames.Role
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Ativa a documentação e descreve quais operações usam o JWT Bearer.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<AuthOperationTransformer>();
    options.AddOperationTransformer<BloqueioAgendaOperationTransformer>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Exibe a documentação e seu script de autenticação somente em desenvolvimento.
    app.UseStaticFiles();
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.SchemeName)
        .WithJavaScriptConfiguration("/scalar-config.js"));
}

app.UseHttpsRedirection();

// Confere o usuário antes de permitir o acesso às rotas.
app.UseAuthentication();
app.UseAuthorization();

// Liga as rotas aos métodos dos controladores.
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

// Mantém o schema sincronizado em cada deploy/cold start. Quando não há migrations pendentes,
// o EF apenas consulta o histórico e segue sem alterações.
await ApplyDatabaseMigrationsAsync(connectionString);

app.Run();

static async Task ApplyDatabaseMigrationsAsync(string connectionString)
{
    var options = new DbContextOptionsBuilder<AgendaiFisioDbContext>()
        .UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure())
        .Options;

    await using var db = new AgendaiFisioDbContext(options);
    await db.Database.MigrateAsync();
}
