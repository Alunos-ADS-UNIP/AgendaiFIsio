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
        private const string MensagemDuplicada = "Já existe uma especialidade cadastrada com este nome.";

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

        public async Task<EspecialidadeResponseDTO> CriarAsync(EspecialidadeCreateDTO dto)
        {
            var nome = dto.Nome.Trim();

            // A comparação do SQL Server ignora maiúsculas e acentos, então "Ortopedia" e "ortopedia" contam como iguais.
            var jaExiste = await _context.Especialidades.AnyAsync(e => e.Nome == nome);

            if (jaExiste)
            {
                throw new InvalidOperationException(MensagemDuplicada);
            }

            var especialidade = new Entities.Especialidade { Nome = nome };
            _context.Especialidades.Add(especialidade);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Cobre o caso de dois cadastros iguais chegarem ao mesmo tempo e o índice único barrar o segundo.
                throw new InvalidOperationException(MensagemDuplicada);
            }

            return new EspecialidadeResponseDTO { Id = especialidade.Id, Nome = especialidade.Nome };
        }
    }
}
