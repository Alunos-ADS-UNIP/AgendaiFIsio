using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.DTOs.Especialidade;
using AgendaiFisio.Repositorio.Interfaces;

namespace AgendaiFisio.Services.Especialidade
{
    public class EspecialidadeService : IEspecialidadeService
    {
        private readonly IEspecialidadeRepositorio _especialidades;

        public EspecialidadeService(IEspecialidadeRepositorio especialidades)
        {
            _especialidades = especialidades;
        }

        public async Task<List<EspecialidadeResponseDTO>> ListarAsync()
        {
            return await _especialidades.Consultar()
                .AsNoTracking()
                .OrderBy(e => e.Nome)
                .Select(e => new EspecialidadeResponseDTO { Id = e.Id, Nome = e.Nome })
                .ToListAsync();
        }

        public async Task<EspecialidadeResponseDTO?> ObterPorIdAsync(Guid id)
        {
            return await _especialidades.Consultar()
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new EspecialidadeResponseDTO { Id = e.Id, Nome = e.Nome })
                .FirstOrDefaultAsync();
        }

    }
}
