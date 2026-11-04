using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.Context;
using AgendaiFisio.DTOs.Especialidade;

namespace AgendaiFisio.Services.Especialidade
{
    public class EspecialidadeService : IEspecialidadeService
    {
        private readonly AgendaiFisioDbContext _context;

        public EspecialidadeService(AgendaiFisioDbContext context)
        {
            _context = context;
        }

        public async Task<List<EspecialidadeResponseDTO>> ListarAsync()
        {
            return await _context.Especialidades
                .AsNoTracking()
                .OrderBy(e => e.Nome)
                .Select(e => new EspecialidadeResponseDTO { Id = e.Id, Nome = e.Nome })
                .ToListAsync();
        }

        public async Task<EspecialidadeResponseDTO?> ObterPorIdAsync(Guid id)
        {
            return await _context.Especialidades
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new EspecialidadeResponseDTO { Id = e.Id, Nome = e.Nome })
                .FirstOrDefaultAsync();
        }

    }
}
