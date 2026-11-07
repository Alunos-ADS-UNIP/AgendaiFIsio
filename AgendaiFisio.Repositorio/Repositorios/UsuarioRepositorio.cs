using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using AgendaiFisio.Repositorio.Interfaces;

namespace AgendaiFisio.Repositorio.Repositorios;

public sealed class UsuarioRepositorio(AgendaiFisioDbContext contexto)
    : RepositorioBase<Usuario>(contexto), IUsuarioRepositorio;
