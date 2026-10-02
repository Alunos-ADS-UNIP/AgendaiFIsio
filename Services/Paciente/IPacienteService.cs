using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Paciente;

namespace AgendaiFisio.Services.Paciente
{
    // Lista as ações disponíveis para pacientes.
   public interface IPacienteService
    {
          // Busca um paciente pelo seu identificador.
        Task<Entities.Paciente?> GetPacienteByIdAsync(Guid id);
          // Atualiza os dados do paciente.
        Task<bool> UpdatePacienteAsync(Guid usuarioId, PacienteUpdateDTO dto);
    }
}