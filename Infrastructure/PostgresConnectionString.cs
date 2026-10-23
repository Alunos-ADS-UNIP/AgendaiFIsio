using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AgendaiFisio.Infrastructure;

/// <summary>
/// Aceita tanto o formato chave/valor do Npgsql quanto URLs postgres:// fornecidas pela Neon e
/// pelo Render, sem registrar a credencial em logs ou arquivos versionados.
/// </summary>
public static class PostgresConnectionString
{
    public static string Resolve(
        IConfiguration configuration,
        string environmentVariable,
        string? fallback = null)
    {
        var value = configuration[environmentVariable];

        if (string.IsNullOrWhiteSpace(value))
        {
            value = fallback ?? configuration.GetConnectionString("AgendaiFisioDbContext");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Configure {environmentVariable} ou ConnectionStrings:AgendaiFisioDbContext.");
        }

        value = value.Trim();
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        return IsPostgresUri(value) ? ConvertUri(value) : value;
    }

    private static bool IsPostgresUri(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static string ConvertUri(string value)
    {
        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);

        if (userInfo.Length != 2)
        {
            throw new InvalidOperationException("A URL do PostgreSQL não contém usuário e senha.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1]),
            SslMode = SslMode.Require,
            Pooling = true,
            ApplicationName = "AgendaiFisioAPI"
        };

        return builder.ConnectionString;
    }
}
