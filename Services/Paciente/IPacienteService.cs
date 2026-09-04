using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Paciente;

namespace AgendaiFisio.Services.Paciente
{
   public interface IPacienteService
    {
        Task<Entities.Paciente> GetPacienteByIdAsync(Guid id);
        Task<bool> UpdatePacienteAsync(Guid usuarioId, PacienteUpdateDTO dto);
    }
}