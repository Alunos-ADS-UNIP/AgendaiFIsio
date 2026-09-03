using System;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Profissional;

namespace AgendaiFisio.Services.Profissional
{
    public interface IProfissionalService
    {
        Task<ProfissionalResponseDTO> GetProfissionalByIdAsync(Guid id);
        Task<ProfissionalResponseDTO> CreateProfissionalAsync(ProfissionalCreateDTO profissional);
        Task<ProfissionalResponseDTO> UpdateProfissionalAsync(Guid id, ProfissionalUpdateDTO profissional);
    }
}