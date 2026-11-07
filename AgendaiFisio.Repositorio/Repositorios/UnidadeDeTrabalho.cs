using AgendaiFisio.Context;
using AgendaiFisio.Repositorio.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AgendaiFisio.Repositorio.Repositorios;

public sealed class UnidadeDeTrabalho(AgendaiFisioDbContext contexto) : IUnidadeDeTrabalho
{
    public async Task SalvarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlException)
        {
            var codigo = sqlException.Number switch
            {
                2601 or 2627 => CodigoConflitoRepositorio.Unicidade,
                51001 => CodigoConflitoRepositorio.AgendamentoEmBloqueio,
                51002 => CodigoConflitoRepositorio.BloqueioComConsulta,
                51003 => CodigoConflitoRepositorio.BloqueioSobreposto,
                _ => null
            };

            if (codigo is not null)
                throw new ConflitoRepositorioException(codigo, ex);

            throw;
        }
    }
}
