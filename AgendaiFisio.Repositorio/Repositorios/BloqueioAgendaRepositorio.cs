using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using AgendaiFisio.Repositorio.Interfaces;

namespace AgendaiFisio.Repositorio.Repositorios;

public sealed class BloqueioAgendaRepositorio(AgendaiFisioDbContext contexto)
    : RepositorioBase<BloqueioAgenda>(contexto), IBloqueioAgendaRepositorio;
