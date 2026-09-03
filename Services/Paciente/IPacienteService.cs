using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Paciente;

namespace AgendaiFisio.Services.Paciente
{
   public interface IPacienteService
    {
        Task<PacienteResponseDTO> GetPacienteByIdAsync(Guid id);
        
        Task<PacienteResponseDTO> CreatePacienteAsync(PacienteCreateDTO pacienteCreateDTO);
        
        Task<PacienteResponseDTO> UpdatePacienteAsync(Guid id, PacienteUpdateDTO pacienteUpdateDTO);
    }
}