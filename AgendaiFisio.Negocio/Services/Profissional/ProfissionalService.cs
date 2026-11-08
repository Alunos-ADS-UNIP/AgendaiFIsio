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
using Microsoft.Extensions.Logging;

namespace AgendaiFisio.Services.Profissional
{
    public class ProfissionalService : IProfissionalService
    {
        private readonly IProfissionalRepositorio _profissionais;
        private readonly IEspecialidadeRepositorio _especialidades;
        private readonly IBloqueioAgendaRepositorio _bloqueios;
        private readonly IAgendamentoRepositorio _agendamentos;
        private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
        private readonly IArmazenamentoFotoPerfil _armazenamentoFotos;
        private readonly ILogger<ProfissionalService> _logger;

        public ProfissionalService(
            IProfissionalRepositorio profissionais,
            IEspecialidadeRepositorio especialidades,
            IBloqueioAgendaRepositorio bloqueios,
            IAgendamentoRepositorio agendamentos,
            IUnidadeDeTrabalho unidadeDeTrabalho,
            IArmazenamentoFotoPerfil armazenamentoFotos,
            ILogger<ProfissionalService> logger)
        {
            _profissionais = profissionais;
            _especialidades = especialidades;
            _bloqueios = bloqueios;
            _agendamentos = agendamentos;
            _unidadeDeTrabalho = unidadeDeTrabalho;
            _armazenamentoFotos = armazenamentoFotos;
            _logger = logger;
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

            var registros = await query
                .OrderBy(p => p.NomeCompleto)
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina)
                .Select(p => new
                {
                    p.Id,
                    p.NomeCompleto,
                    Especialidade = p.Especialidade == null
                        ? null
                        : new EspecialidadeResponseDTO { Id = p.Especialidade.Id, Nome = p.Especialidade.Nome },
                    p.Crefito,
                    p.FotoPerfilChave,
                    p.Ativo,
                    Endereco = p.Endereco == null ? null : new EnderecoProfissionalDTO
                    {
                        Rua = p.Endereco.Rua,
                        Numero = p.Endereco.Numero,
                        Complemento = p.Endereco.Complemento,
                        Cep = p.Endereco.Cep,
                        Bairro = p.Endereco.Bairro,
                        Cidade = p.Endereco.Cidade,
                        Estado = p.Endereco.Estado
                    }
                })
                .ToListAsync();

            var itens = registros.Select(p => new ProfissionalListItemDTO
            {
                Id = p.Id,
                NomeCompleto = p.NomeCompleto,
                Especialidade = p.Especialidade,
                Crefito = p.Crefito,
                FotoPerfilUrl = GerarUrlFoto(p.FotoPerfilChave),
                Ativo = p.Ativo,
                Endereco = p.Endereco
            }).ToList();

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
                .Include(p => p.Endereco)
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
                FotoPerfilUrl = GerarUrlFoto(profissional.FotoPerfilChave),
                DataCadastro = profissional.DataCadastro,
                Ativo = profissional.Ativo,
                Email = profissional.Usuario?.Email ?? string.Empty,
                Endereco = MapearEndereco(profissional.Endereco)
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
                .Include(p => p.Endereco)
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
                FotoPerfilUrl = GerarUrlFoto(profissional.FotoPerfilChave),
                DataCadastro = profissional.DataCadastro,
                Ativo = profissional.Ativo,
                Email = profissional.Usuario?.Email ?? string.Empty,
                Endereco = MapearEndereco(profissional.Endereco),
                Cpf = profissional.Cpf,
                DataNascimento = profissional.DataNascimento.Year > 1
                    ? DateTime.SpecifyKind(profissional.DataNascimento, DateTimeKind.Unspecified)
                    : null
            };
        }

        public async Task AtualizarAsync(Guid usuarioId, ProfissionalUpdateDTO dto)
        {
            var profissional = await _profissionais.Consultar()
                .Include(p => p.Endereco)
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
            profissional.Endereco ??= new Entities.Endereco();
            profissional.Endereco.Rua = dto.Rua.Trim();
            profissional.Endereco.Numero = dto.Numero.Trim();
            profissional.Endereco.Complemento = dto.Complemento?.Trim() ?? string.Empty;
            profissional.Endereco.Cep = dto.Cep.Trim();
            profissional.Endereco.Bairro = dto.Bairro.Trim();
            profissional.Endereco.Cidade = dto.Cidade.Trim();
            profissional.Endereco.Estado = dto.Estado.Trim().ToUpperInvariant();

            await _unidadeDeTrabalho.SalvarAsync();
        }

        private static EnderecoProfissionalDTO? MapearEndereco(Entities.Endereco? endereco)
        {
            if (endereco is null) return null;
            return new EnderecoProfissionalDTO
            {
                Rua = endereco.Rua,
                Numero = endereco.Numero,
                Complemento = endereco.Complemento,
                Cep = endereco.Cep,
                Bairro = endereco.Bairro,
                Cidade = endereco.Cidade,
                Estado = endereco.Estado
            };
        }

        public async Task<FotoPerfilResponseDTO> AtualizarFotoPerfilAsync(
            Guid usuarioId,
            Stream conteudo,
            long tamanho,
            string tipoConteudo,
            CancellationToken cancellationToken = default)
        {
            if (tamanho <= 0)
                throw new ArgumentException("Selecione uma imagem para enviar.");
            if (tamanho > _armazenamentoFotos.TamanhoMaximoBytes)
                throw new ArgumentException($"A foto deve ter no máximo {_armazenamentoFotos.TamanhoMaximoBytes / 1024 / 1024} MB.");

            var profissional = await _profissionais.Consultar()
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, cancellationToken);
            if (profissional is null)
                throw new KeyNotFoundException("Fisioterapeuta não encontrado para o usuário informado.");

            await using var imagem = new MemoryStream((int)tamanho);
            await conteudo.CopyToAsync(imagem, cancellationToken);
            if (imagem.Length > _armazenamentoFotos.TamanhoMaximoBytes)
                throw new ArgumentException($"A foto deve ter no máximo {_armazenamentoFotos.TamanhoMaximoBytes / 1024 / 1024} MB.");

            var tipoDetectado = DetectarTipoImagem(imagem.GetBuffer().AsSpan(0, (int)imagem.Length));
            if (tipoDetectado is null)
                throw new ArgumentException("O arquivo deve ser uma imagem JPEG, PNG ou WebP válida.");
            if (!string.Equals(tipoConteudo, tipoDetectado, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("O conteúdo do arquivo não corresponde ao tipo de imagem informado.");

            imagem.Position = 0;
            var chaveAnterior = profissional.FotoPerfilChave;
            var novaChave = await _armazenamentoFotos.SalvarAsync(
                profissional.Id, imagem, tipoDetectado, cancellationToken);

            profissional.FotoPerfilChave = novaChave;
            try
            {
                await _unidadeDeTrabalho.SalvarAsync(cancellationToken);
            }
            catch
            {
                await TentarExcluirAsync(novaChave);
                throw;
            }

            if (!string.IsNullOrWhiteSpace(chaveAnterior))
                await TentarExcluirAsync(chaveAnterior);

            return new FotoPerfilResponseDTO
            {
                Mensagem = "Foto de perfil atualizada com sucesso.",
                FotoPerfilUrl = GerarUrlFoto(novaChave)
            };
        }

        public async Task RemoverFotoPerfilAsync(
            Guid usuarioId,
            CancellationToken cancellationToken = default)
        {
            var profissional = await _profissionais.Consultar()
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, cancellationToken);
            if (profissional is null)
                throw new KeyNotFoundException("Fisioterapeuta não encontrado para o usuário informado.");

            var chaveAnterior = profissional.FotoPerfilChave;
            profissional.FotoPerfilChave = null;
            await _unidadeDeTrabalho.SalvarAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(chaveAnterior))
                await TentarExcluirAsync(chaveAnterior);
        }

        private string? GerarUrlFoto(string? chave) =>
            string.IsNullOrWhiteSpace(chave) ? null : _armazenamentoFotos.GerarUrlLeitura(chave);

        private async Task TentarExcluirAsync(string chave)
        {
            try
            {
                await _armazenamentoFotos.ExcluirAsync(chave, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível remover o objeto S3 {FotoPerfilChave}.", chave);
            }
        }

        private static string? DetectarTipoImagem(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return "image/jpeg";
            if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                return "image/png";
            if (bytes.Length >= 12 &&
                bytes[..4].SequenceEqual("RIFF"u8) &&
                bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
                return "image/webp";
            return null;
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
