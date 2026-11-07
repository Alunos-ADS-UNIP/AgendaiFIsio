using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AgendaiFisio.Context;

public sealed class AgendaiFisioDbContextFactory : IDesignTimeDbContextFactory<AgendaiFisioDbContext>
{
    public AgendaiFisioDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var apiPath = Path.Combine(basePath, "AgendaiFisio.Api");
        if (File.Exists(Path.Combine(apiPath, "appsettings.json")))
            basePath = apiPath;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Padrao");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Padrao' não encontrada. Configure ConnectionStrings__Padrao.");
        }
        var options = new DbContextOptionsBuilder<AgendaiFisioDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AgendaiFisioDbContext(options);
    }
}
