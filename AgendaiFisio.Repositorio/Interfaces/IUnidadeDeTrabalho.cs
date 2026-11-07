namespace AgendaiFisio.Repositorio.Interfaces;

public interface IUnidadeDeTrabalho
{
    Task SalvarAsync(CancellationToken cancellationToken = default);
}
