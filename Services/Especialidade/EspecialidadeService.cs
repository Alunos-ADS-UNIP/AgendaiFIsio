using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.Context;
using AgendaiFisio.DTOs.Especialidade;

namespace AgendaiFisio.Services.Especialidade
{
    public class EspecialidadeService : IEspecialidadeService
    {
        private const string MensagemDuplicada = "Já existe uma especialidade cadastrada com este nome.";

        // Números de erro do SQL Server para violação de índice/restrição única.
        private const int SqlErrorIndiceUnicoDuplicado = 2601;
        private const int SqlErrorChaveUnicaDuplicada = 2627;

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

        public async Task<EspecialidadeResponseDTO> CriarAsync(EspecialidadeCreateDTO dto)
        {
            // Normaliza antes de validar o tamanho: " a " não pode passar como nome de 3 caracteres.
            var nome = EspecialidadeNomeNormalizador.Normalizar(dto.Nome);

            if (nome.Length < 3)
            {
                throw new ArgumentException("O nome deve ter entre 3 e 100 caracteres.");
            }

            // A collation da coluna Nome (Latin1_General_CI_AI) já ignora maiúsculas e acentos aqui.
            var jaExiste = await _context.Especialidades.AnyAsync(e => e.Nome == nome);

            if (jaExiste)
            {
                throw new InvalidOperationException(MensagemDuplicada);
            }

            // Guarda a grafia aprovada (maiúsculas/acentos do jeito que o administrador digitou),
            // só com os espaços normalizados — a comparação de duplicidade é que ignora caixa/acento.
            var especialidade = new Entities.Especialidade { Nome = nome };
            _context.Especialidades.Add(especialidade);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (EhViolacaoDeChaveUnica(ex))
            {
                // Cobre o caso de dois cadastros iguais chegarem ao mesmo tempo e o índice único
                // barrar o segundo depois que a checagem acima já tinha passado.
                throw new InvalidOperationException(MensagemDuplicada);
            }

            return new EspecialidadeResponseDTO { Id = especialidade.Id, Nome = especialidade.Nome };
        }

        // Só os erros 2601/2627 (violação de índice ou restrição única) significam nome
        // duplicado; qualquer outra falha de banco deve seguir como erro interno de verdade,
        // não como "nome já existe".
        private static bool EhViolacaoDeChaveUnica(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlEx
                && (sqlEx.Number == SqlErrorIndiceUnicoDuplicado || sqlEx.Number == SqlErrorChaveUnicaDuplicada);
        }
    }
}
