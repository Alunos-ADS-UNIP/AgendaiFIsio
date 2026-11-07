using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.DTOs;
using AgendaiFisio.DTOs.Especialidade;
using AgendaiFisio.DTOs.Profissional;
using AgendaiFisio.Constants;
using AgendaiFisio.Repositorio.Interfaces;

namespace AgendaiFisio.Services.Profissional
{
    public class ProfissionalService : IProfissionalService
    {
        private readonly IProfissionalRepositorio _profissionais;
        private readonly IEspecialidadeRepositorio _especialidades;
        private readonly IBloqueioAgendaRepositorio _bloqueios;
        private readonly IAgendamentoRepositorio _agendamentos;
        private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

        public ProfissionalService(
            IProfissionalRepositorio profissionais,
            IEspecialidadeRepositorio especialidades,
            IBloqueioAgendaRepositorio bloqueios,
            IAgendamentoRepositorio agendamentos,
            IUnidadeDeTrabalho unidadeDeTrabalho)
        {
            _profissionais = profissionais;
            _especialidades = especialidades;
            _bloqueios = bloqueios;
            _agendamentos = agendamentos;
            _unidadeDeTrabalho = unidadeDeTrabalho;
        }

        public async Task<PagedResultDTO<ProfissionalListItemDTO>> ListarAsync(ProfissionalFiltroDTO filtro)
        {
            var query = _profissionais.Consultar().AsNoTracking();

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
            var profissional = await _profissionais.Consultar()
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

        // Uma consulta ocupa uma hora (as consultas sempre começam em hora cheia).
        private static readonly TimeSpan DuracaoConsulta = TimeSpan.FromHours(1);
        private static readonly TimeSpan JanelaMaximaIndisponibilidade = TimeSpan.FromDays(93);

        public async Task<IReadOnlyList<IndisponibilidadeDTO>> ListarIndisponibilidadeAsync(
            Guid profissionalId,
            DateTime inicioUtc,
            DateTime fimUtc)
        {
            if (inicioUtc.Kind != DateTimeKind.Utc || fimUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("O período deve estar em UTC.");
            if (fimUtc <= inicioUtc)
                throw new ArgumentException("O fim do período deve ser posterior ao início.");
            if (fimUtc - inicioUtc > JanelaMaximaIndisponibilidade)
                throw new ArgumentException("O período consultado deve ter no máximo 93 dias.");

            var profissionalAtivo = await _profissionais.Consultar()
                .AnyAsync(p => p.Id == profissionalId && p.Ativo);
            if (!profissionalAtivo)
                throw new KeyNotFoundException("Fisioterapeuta não encontrado.");

            var bloqueios = await _bloqueios.Consultar().AsNoTracking()
                .Where(b => b.ProfissionalId == profissionalId &&
                            b.Inicio < fimUtc &&
                            b.Fim > inicioUtc)
                .Select(b => new { b.Inicio, b.Fim })
                .ToListAsync();

            var inicioConsultas = inicioUtc - DuracaoConsulta;
            var consultas = await _agendamentos.Consultar().AsNoTracking()
                .Where(a => a.ProfissionalId == profissionalId &&
                            a.Status != StatusAgendamento.Cancelado &&
                            a.DataHora > inicioConsultas &&
                            a.DataHora < fimUtc)
                .Select(a => a.DataHora)
                .ToListAsync();

            return bloqueios
                .Select(b => new IndisponibilidadeDTO { Inicio = Utc(b.Inicio), Fim = Utc(b.Fim) })
                .Concat(consultas.Select(inicio => new IndisponibilidadeDTO
                {
                    Inicio = Utc(inicio),
                    Fim = Utc(inicio + DuracaoConsulta)
                }))
                .OrderBy(p => p.Inicio)
                .ToList();
        }

        private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        public async Task<ProfissionalMeDTO> ObterMeuPerfilAsync(Guid usuarioId)
        {
            var profissional = await _profissionais.Consultar()
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.Especialidade)
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (profissional is null)
            {
                throw new KeyNotFoundException("Fisioterapeuta não encontrado.");
            }

            return new ProfissionalMeDTO
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
                Email = profissional.Usuario?.Email ?? string.Empty,
                Cpf = profissional.Cpf,
                DataNascimento = profissional.DataNascimento.Year > 1
                    ? DateTime.SpecifyKind(profissional.DataNascimento, DateTimeKind.Unspecified)
                    : null
            };
        }

        public async Task AtualizarAsync(Guid usuarioId, ProfissionalUpdateDTO dto)
        {
            var profissional = await _profissionais.Consultar()
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (profissional is null)
            {
                throw new KeyNotFoundException("Fisioterapeuta não encontrado para o usuário informado.");
            }

            var crefito = dto.Crefito.Trim();

            // Impede que dois fisioterapeutas fiquem com o mesmo CREFITO.
            var crefitoEmUso = await _profissionais.Consultar()
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

            await _unidadeDeTrabalho.SalvarAsync();
        }

        public async Task AtualizarEspecialidadeAsync(Guid usuarioId, Guid especialidadeId)
        {
            var profissional = await _profissionais.Consultar()
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (profissional is null)
            {
                throw new KeyNotFoundException("Fisioterapeuta não encontrado para o usuário informado.");
            }

            if (especialidadeId == Guid.Empty)
            {
                throw new ArgumentException("Especialidade inválida.");
            }

            var especialidadeExiste = await _especialidades.Consultar()
                .AnyAsync(e => e.Id == especialidadeId);

            if (!especialidadeExiste)
            {
                throw new ArgumentException("Especialidade informada não existe no catálogo.");
            }

            // Reenviar o mesmo id mantém um único vínculo; outro id substitui a FK atomicamente.
            profissional.EspecialidadeId = especialidadeId;

            await _unidadeDeTrabalho.SalvarAsync();
        }
    }
}
