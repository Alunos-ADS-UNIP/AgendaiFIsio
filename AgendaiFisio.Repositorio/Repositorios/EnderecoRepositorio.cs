using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using AgendaiFisio.Repositorio.Interfaces;

namespace AgendaiFisio.Repositorio.Repositorios;

public sealed class EnderecoRepositorio(AgendaiFisioDbContext contexto)
    : RepositorioBase<Endereco>(contexto), IEnderecoRepositorio;
