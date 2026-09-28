using System.Collections.Generic;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Especialidade;

namespace AgendaiFisio.Services.Especialidade
{
    public interface IEspecialidadeService
    {
        // Devolve todas as especialidades cadastradas, em ordem alfabética.
        Task<List<EspecialidadeResponseDTO>> ListarAsync();

        // Cadastra uma especialidade nova, recusando nomes já existentes.
        Task<EspecialidadeResponseDTO> CriarAsync(EspecialidadeCreateDTO dto);
    }
}
