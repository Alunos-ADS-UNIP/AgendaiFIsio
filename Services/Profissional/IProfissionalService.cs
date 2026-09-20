using System;
using System.Threading.Tasks;
using AgendaiFisio.DTOs;
using AgendaiFisio.DTOs.Profissional;

namespace AgendaiFisio.Services.Profissional
{
    public interface IProfissionalService
    {
        // Devolve a lista de fisioterapeutas já filtrada e paginada.
        Task<PagedResultDTO<ProfissionalListItemDTO>> ListarAsync(ProfissionalFiltroDTO filtro);

        // Devolve os dados completos de um fisioterapeuta pelo id.
        Task<ProfissionalDetailDTO> ObterPorIdAsync(Guid id);

        // Atualiza o perfil do fisioterapeuta identificado pelo usuário logado.
        Task AtualizarAsync(Guid usuarioId, ProfissionalUpdateDTO dto);
    }
}