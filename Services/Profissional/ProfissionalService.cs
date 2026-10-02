using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.DTOs;
using AgendaiFisio.DTOs.Especialidade;
using AgendaiFisio.DTOs.Profissional;

using AgendaiFisio.Context;

namespace AgendaiFisio.Services.Profissional
{
    public class ProfissionalService : IProfissionalService
    {
        private readonly AgendaiFisioDbContext _context;

        public ProfissionalService(AgendaiFisioDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDTO<ProfissionalListItemDTO>> ListarAsync(ProfissionalFiltroDTO filtro)
        {
            var query = _context.Profissionais.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro.Nome))
            {
                var nome = filtro.Nome.Trim().ToLower();
                query = query.Where(p => p.NomeCompleto.ToLower().Contains(nome));
            }

            if (filtro.EspecialidadeId.HasValue)
            {
                // Sem este filtro, profissionais sem especialidade escolhida continuam aparecendo.
                query = query.Where(p => p.EspecialidadeId == filtro.EspecialidadeId.Value);
            }

            if (filtro.Ativo.HasValue)
            {
                query = query.Where(p => p.Ativo == filtro.Ativo.Value);
            }

            var totalRegistros = await query.CountAsync();

            var itens = await query
                .OrderBy(p => p.NomeCompleto)
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina)
                .Select(p => new ProfissionalListItemDTO
                {
                    Id = p.Id,
                    NomeCompleto = p.NomeCompleto,
                    Especialidade = p.Especialidade == null
                        ? null
                        : new EspecialidadeResponseDTO { Id = p.Especialidade.Id, Nome = p.Especialidade.Nome },
                    Crefito = p.Crefito,
                    Ativo = p.Ativo
                })
                .ToListAsync();

            return new PagedResultDTO<ProfissionalListItemDTO>
            {
                Itens = itens,
                TotalRegistros = totalRegistros,
                PaginaAtual = filtro.Pagina,
                TamanhoPagina = filtro.TamanhoPagina
            };
        }

        public async Task<ProfissionalDetailDTO> ObterPorIdAsync(Guid id)
        {
            var profissional = await _context.Profissionais
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.Especialidade)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profissional is null)
            {
                throw new KeyNotFoundException("Fisioterapeuta não encontrado.");
            }

            return new ProfissionalDetailDTO
            {
                Id = profissional.Id,
                NomeCompleto = profissional.NomeCompleto,
                Crefito = profissional.Crefito,
                Telefone = profissional.Telefone,
                Especialidade = profissional.Especialidade is null
                    ? null
                    : new EspecialidadeResponseDTO { Id = profissional.Especialidade.Id, Nome = profissional.Especialidade.Nome },
                Bio = profissional.Bio,
                DataCadastro = profissional.DataCadastro,
                Ativo = profissional.Ativo,
                Email = profissional.Usuario?.Email ?? string.Empty
            };
        }

        public async Task AtualizarAsync(Guid usuarioId, ProfissionalUpdateDTO dto)
        {
            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (profissional is null)
            {
                throw new KeyNotFoundException("Fisioterapeuta não encontrado para o usuário informado.");
            }

            var crefito = dto.Crefito.Trim();

            // Impede que dois fisioterapeutas fiquem com o mesmo CREFITO.
            var crefitoEmUso = await _context.Profissionais
                .AnyAsync(p => p.Id != profissional.Id && p.Crefito == crefito);

            if (crefitoEmUso)
            {
                throw new InvalidOperationException("Já existe outro fisioterapeuta cadastrado com este CREFITO.");
            }

            // A especialidade não é alterada aqui — ver AtualizarEspecialidadeAsync. Omitir o
            // campo neste PUT nunca mexe no vínculo já salvo.
            profissional.NomeCompleto = dto.NomeCompleto.Trim();
            profissional.Cpf = dto.Cpf.Trim();
            profissional.Crefito = crefito;
            profissional.Telefone = dto.Telefone.Trim();
            profissional.DataNascimento = dto.DataNascimento;
            profissional.Bio = dto.Bio?.Trim() ?? string.Empty;
            profissional.Ativo = dto.Ativo;

            await _context.SaveChangesAsync();
        }

        public async Task AtualizarEspecialidadeAsync(Guid usuarioId, Guid especialidadeId)
        {
            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (profissional is null)
            {
                throw new KeyNotFoundException("Fisioterapeuta não encontrado para o usuário informado.");
            }

            if (especialidadeId == Guid.Empty)
            {
                throw new ArgumentException("Especialidade inválida.");
            }

            var especialidadeExiste = await _context.Especialidades.AnyAsync(e => e.Id == especialidadeId);

            if (!especialidadeExiste)
            {
                throw new ArgumentException("Especialidade informada não existe no catálogo.");
            }

            // Reenviar o mesmo id mantém um único vínculo; outro id substitui a FK atomicamente.
            profissional.EspecialidadeId = especialidadeId;

            await _context.SaveChangesAsync();
        }
    }
}
