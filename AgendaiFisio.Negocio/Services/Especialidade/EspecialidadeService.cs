using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.DTOs.Especialidade;
using AgendaiFisio.Repositorio;
using AgendaiFisio.Repositorio.Interfaces;

namespace AgendaiFisio.Services.Especialidade
{
    public class EspecialidadeService : IEspecialidadeService
    {
        private const string MensagemDuplicada = "Já existe uma especialidade cadastrada com este nome.";

        private readonly IEspecialidadeRepositorio _especialidades;
        private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

        public EspecialidadeService(
            IEspecialidadeRepositorio especialidades,
            IUnidadeDeTrabalho unidadeDeTrabalho)
        {
            _especialidades = especialidades;
            _unidadeDeTrabalho = unidadeDeTrabalho;
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

        public async Task<EspecialidadeResponseDTO> CriarAsync(EspecialidadeCreateDTO dto)
        {
            // Normaliza antes de validar o tamanho: " a " não pode passar como nome de 3 caracteres.
            var nome = EspecialidadeNomeNormalizador.Normalizar(dto.Nome);

            if (nome.Length < 3)
            {
                throw new ArgumentException("O nome deve ter entre 3 e 100 caracteres.");
            }

            // A collation do SQL Server já ignora maiúsculas e acentos aqui.
            var jaExiste = await _especialidades.Consultar().AnyAsync(e => e.Nome == nome);

            if (jaExiste)
            {
                throw new InvalidOperationException(MensagemDuplicada);
            }

            // Guarda a grafia aprovada (maiúsculas/acentos do jeito que o administrador digitou),
            // só com os espaços normalizados — a comparação de duplicidade é que ignora caixa/acento.
            var especialidade = new Entities.Especialidade { Nome = nome };
            _especialidades.Adicionar(especialidade);

            try
            {
                await _unidadeDeTrabalho.SalvarAsync();
            }
            catch (ConflitoRepositorioException ex) when (
                ex.Codigo == CodigoConflitoRepositorio.Unicidade)
            {
                // Cobre o caso de dois cadastros iguais chegarem ao mesmo tempo e o índice único
                // barrar o segundo depois que a checagem acima já tinha passado.
                throw new InvalidOperationException(MensagemDuplicada);
            }

            return new EspecialidadeResponseDTO { Id = especialidade.Id, Nome = especialidade.Nome };
        }
    }
}
