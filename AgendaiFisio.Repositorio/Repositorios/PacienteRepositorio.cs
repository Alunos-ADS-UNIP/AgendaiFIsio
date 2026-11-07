using AgendaiFisio.Context;
using AgendaiFisio.Entities;
using AgendaiFisio.Repositorio.Interfaces;

namespace AgendaiFisio.Repositorio.Repositorios;

public sealed class PacienteRepositorio(AgendaiFisioDbContext contexto)
    : RepositorioBase<Paciente>(contexto), IPacienteRepositorio;
