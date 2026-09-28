using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using AgendaiFisio.Context;
using AgendaiFisio.Services.Auth;
using AgendaiFisio.Services.Especialidade;
using AgendaiFisio.Services.Paciente;
using AgendaiFisio.Services.Profissional;

var builder = WebApplication.CreateBuilder(args);

// Configura o acesso ao banco de dados.
builder.Services.AddDbContext<AgendaiFisioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AgendaiFisioDbContext")));

// Registra os serviços usados pela aplicação.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IProfissionalService, ProfissionalService>();
builder.Services.AddScoped<IEspecialidadeService, EspecialidadeService>();

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
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Ativa a documentação da API.
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Exibe a documentação enquanto o sistema está em desenvolvimento.
    app.MapOpenApi(); 
    app.MapScalarApiReference(); 
}

app.UseHttpsRedirection();

// Confere o usuário antes de permitir o acesso às rotas.
app.UseAuthentication();
app.UseAuthorization();

// Liga as rotas aos métodos dos controladores.
app.MapControllers();

app.Run();