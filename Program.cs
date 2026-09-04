using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using AgendaiFisio.Context;
using AgendaiFisio.Services.Auth;
using AgendaiFisio.Services.Paciente;
using AgendaiFisio.Services.Profissional;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Banco de Dados
builder.Services.AddDbContext<AgendaiFisioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AgendaiFisioDbContext")));

// 2. Injeção de Dependência (DI) dos Serviços
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IProfissionalService, ProfissionalService>(); 

// 3. Configuração da Autenticação JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey");

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


builder.Services.AddOpenApi();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); 
    app.MapScalarApiReference(); 
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();