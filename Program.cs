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
using AgendaiFisio.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// O Render fornece a porta em tempo de execução. Localmente, o launchSettings continua valendo.
if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connectionString = PostgresConnectionString.Resolve(
    builder.Configuration,
    "DATABASE_URL");
var migrationConnectionString = PostgresConnectionString.Resolve(
    builder.Configuration,
    "DATABASE_URL_UNPOOLED",
    connectionString);

// Usa a URL pooled no tráfego normal da API. As migrations usam a URL direta da Neon.
builder.Services.AddDbContext<AgendaiFisioDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

// Registra os serviços usados pela aplicação.
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
await ApplyDatabaseMigrationsAsync(migrationConnectionString);

app.Run();

static async Task ApplyDatabaseMigrationsAsync(string connectionString)
{
    var options = new DbContextOptionsBuilder<AgendaiFisioDbContext>()
        .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
        .Options;

    await using var db = new AgendaiFisioDbContext(options);
    await db.Database.MigrateAsync();
}
