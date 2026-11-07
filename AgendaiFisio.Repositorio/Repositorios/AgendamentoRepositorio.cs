using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using AgendaiFisio.Repositorio.Interfaces;

namespace AgendaiFisio.Repositorio.Repositorios;

public sealed class AgendamentoRepositorio(AgendaiFisioDbContext contexto)
    : RepositorioBase<Agendamento>(contexto), IAgendamentoRepositorio;
