using AgendaiFisio.Repositorio.Interfaces;

namespace AgendaiFisio.Api.Services;

// Executa ao iniciar a aplicação e depois a cada hora. O lote mantém o trabalho
// curto mesmo em hospedagens compartilhadas.
public sealed class DadosDemonstracaoCleanupWorker : BackgroundService
{
    private const int TamanhoLote = 100;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DadosDemonstracaoCleanupWorker> _logger;

    public DadosDemonstracaoCleanupWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DadosDemonstracaoCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await LimparAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao remover dados demonstrativos expirados.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task LimparAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<IDadosDemonstracaoRepositorio>();
        var armazenamento = scope.ServiceProvider.GetRequiredService<IArmazenamentoFotoPerfil>();
        var totalExcluidas = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var contas = await repositorio.ListarExpiradasAsync(
                DateTime.UtcNow,
                TamanhoLote,
                cancellationToken);
            if (contas.Count == 0)
                break;

            // A foto é dado pessoal: só excluímos o registro do banco depois que o
            // objeto correspondente também tiver sido removido do armazenamento.
            foreach (var chave in contas.Select(c => c.FotoPerfilChave).Where(c => !string.IsNullOrWhiteSpace(c)))
                await armazenamento.ExcluirAsync(chave!, cancellationToken);

            var excluidas = await repositorio.ExcluirContasAsync(
                contas.Select(c => c.UsuarioId).ToArray(),
                cancellationToken);
            totalExcluidas += excluidas;
            if (excluidas == 0 || contas.Count < TamanhoLote)
                break;
        }

        if (totalExcluidas > 0)
        {
            _logger.LogInformation(
                "{Quantidade} conta(s) demonstrativa(s) expirada(s) foram removidas.",
                totalExcluidas);
        }
    }
}
