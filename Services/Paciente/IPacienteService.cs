using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.DTOs;

namespace AgendaiFisio.Services.Paciente
{
    // Lista as ações disponíveis para pacientes.
   public interface IPacienteService
    {
          // Busca um paciente pelo seu identificador.
        Task<Entities.Paciente?> GetPacienteByIdAsync(Guid id);
          // Atualiza os dados do paciente.
        Task<bool> UpdatePacienteAsync(Guid usuarioId, PacienteUpdateDTO dto);

        // Retorna agendamentos passados do paciente para ele próprio ou para um profissional vinculado.
        Task<PagedResultDTO<HistoricoConsultaDTO>> ListarHistoricoConsultasAsync(
            Guid pacienteId, Guid usuarioId, string tipoUsuario, HistoricoConsultaFiltroDTO filtro);
    }
}
