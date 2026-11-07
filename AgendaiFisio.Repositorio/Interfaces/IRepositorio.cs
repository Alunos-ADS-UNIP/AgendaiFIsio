namespace AgendaiFisio.Repositorio.Interfaces;

public interface IRepositorio<TEntity> where TEntity : class
{
    IQueryable<TEntity> Consultar();
    void Adicionar(TEntity entidade);
    void Remover(TEntity entidade);
    void RemoverVarios(IEnumerable<TEntity> entidades);
}
