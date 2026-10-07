using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using AgendaiFisio.Constants;
using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using AgendaiFisio.Services.Auth;
using AgendaiFisio.Services.Especialidade;
using AgendaiFisio.Services.Paciente;
using AgendaiFisio.Services.Profissional;
using AgendaiFisio.Services.Agendamento;
using AgendaiFisio.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Configura o acesso ao banco de dados.
builder.Services.AddDbContext<AgendaiFisioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AgendaiFisioDbContext")));

// Registra os serviços usados pela aplicação.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtualService, UsuarioAtualService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IProfissionalService, ProfissionalService>();
builder.Services.AddScoped<IEspecialidadeService, EspecialidadeService>();
builder.Services.AddScoped<IAgendamentoService, AgendamentoService>();

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

// Provisiona a primeira conta Clínica (admin) a partir da configuração, nunca pelo cadastro
// público — é o "mecanismo confiável" exigido pela revisão (achado 2). Sem AdminSeed:Email e
// AdminSeed:Senha configurados (ex.: variáveis de ambiente ou user-secrets, nunca no appsettings
// versionado), este passo não faz nada. Depois do primeiro login, troque a senha e remova a
// configuração.
await SeedContaClinicaInicialAsync(app);

app.Run();

static async Task SeedContaClinicaInicialAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var configuracao = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    var email = configuracao["AdminSeed:Email"];
    var senha = configuracao["AdminSeed:Senha"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
    {
        return;
    }

    var db = scope.ServiceProvider.GetRequiredService<AgendaiFisioDbContext>();
    var emailNormalizado = email.Trim().ToLower();

    var jaExiste = await db.Usuarios.AnyAsync(u => u.Email == emailNormalizado);

    if (jaExiste)
    {
        return;
    }

    db.Usuarios.Add(new Usuario
    {
        Email = emailNormalizado,
        SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha),
        TipoUsuario = PerfilDeUsuario.Admin
    });

    await db.SaveChangesAsync();
}
