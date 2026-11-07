using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using AgendaiFisio.Repositorio.Interfaces;

namespace AgendaiFisio.Repositorio.Repositorios;

public sealed class ProfissionalRepositorio(AgendaiFisioDbContext contexto)
    : RepositorioBase<Profissional>(contexto), IProfissionalRepositorio;
