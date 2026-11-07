using AgendaiFisio.Context;
using AgendaiFisio.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgendaiFisio.Repositorio.Repositorios;

public abstract class RepositorioBase<TEntity>(AgendaiFisioDbContext contexto)
    : IRepositorio<TEntity>
    where TEntity : class
{
    protected DbSet<TEntity> Entidades { get; } = contexto.Set<TEntity>();

    public IQueryable<TEntity> Consultar() => Entidades.AsQueryable();
    public void Adicionar(TEntity entidade) => Entidades.Add(entidade);
    public void Remover(TEntity entidade) => Entidades.Remove(entidade);
    public void RemoverVarios(IEnumerable<TEntity> entidades) => Entidades.RemoveRange(entidades);
}
