using AgendaiFisio.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AgendaiFisio.Context;

public sealed class AgendaiFisioDbContextFactory : IDesignTimeDbContextFactory<AgendaiFisioDbContext>
{
    public AgendaiFisioDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = PostgresConnectionString.Resolve(
            configuration,
            "DATABASE_URL_UNPOOLED");
        var options = new DbContextOptionsBuilder<AgendaiFisioDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AgendaiFisioDbContext(options);
    }
}
