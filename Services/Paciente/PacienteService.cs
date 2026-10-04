using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.Context;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.Entities;
using AgendaiFisio.Constants;

namespace AgendaiFisio.Services.Paciente
{
    // Busca e atualiza os dados dos pacientes.
    public class PacienteService : IPacienteService
    {
        private readonly AgendaiFisioDbContext _context;

        // Guarda o banco usado pelo serviço.
        public PacienteService(AgendaiFisioDbContext context)
        {
            _context = context;
        }

        // Busca um paciente e seu endereço pelo identificador.
        public async Task<Entities.Paciente?> GetPacienteByIdAsync(Guid id)
        {
            return await _context.Pacientes
                .Include(p => p.Endereco)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // Atualiza os dados pessoais e o endereço do paciente.
        public async Task<bool> UpdatePacienteAsync(Guid usuarioId, PacienteUpdateDTO dto)
        {
            // Busca o perfil ligado ao usuário logado.
            var paciente = await _context.Pacientes
                .Include(p => p.Endereco)
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paciente == null)
                throw new KeyNotFoundException("Perfil de paciente não encontrado para este usuário.");

            paciente.NomeCompleto = dto.NomeCompleto;
            paciente.Cpf = dto.Cpf;
            paciente.DataNascimento = dto.DataNascimento;
            paciente.Telefone = dto.Telefone;
            paciente.Sexo = dto.Sexo;
            paciente.EstadoCivil = dto.EstadoCivil;

            if (paciente.Endereco == null)
            {
                // Cria um endereço quando o paciente ainda não possui um.
                paciente.Endereco = new Endereco();
            }

            
            paciente.Endereco.Rua = dto.Rua;
            paciente.Endereco.Numero = dto.Numero;
            paciente.Endereco.Cep = dto.Cep;
            
            
           
            if (dto.Complemento is not null) paciente.Endereco.Complemento = dto.Complemento.Trim();
            if (dto.Bairro is not null) paciente.Endereco.Bairro = dto.Bairro.Trim();
            if (dto.Cidade is not null) paciente.Endereco.Cidade = dto.Cidade.Trim();
            if (dto.Estado is not null) paciente.Endereco.Estado = dto.Estado.Trim();

            // Salva as alterações feitas no perfil.
            await _context.SaveChangesAsync();

            return true;
        }

        // Consulta somente agendamentos passados. O status armazenado é exibido sem presumir
        // que um horário passado foi concluído.
        public async Task<IReadOnlyList<HistoricoConsultaDTO>> ListarHistoricoConsultasAsync(
            Guid pacienteId, Guid usuarioId, string tipoUsuario)
        {
            var pacienteExiste = await _context.Pacientes.AsNoTracking()
                .AnyAsync(p => p.Id == pacienteId);

            if (!pacienteExiste)
                throw new KeyNotFoundException("Paciente não encontrado.");

            var podeConsultar = tipoUsuario switch
            {
                PerfilDeUsuario.Paciente => await _context.Pacientes.AsNoTracking()
                    .AnyAsync(p => p.Id == pacienteId && p.UsuarioId == usuarioId),
                PerfilDeUsuario.Profissional => await _context.Agendamentos.AsNoTracking()
                    .AnyAsync(a => a.PacienteId == pacienteId &&
                        a.Profissional!.UsuarioId == usuarioId && a.Status != "Cancelado"),
                _ => false
            };

            if (!podeConsultar)
                throw new UnauthorizedAccessException("Você não pode consultar o histórico deste paciente.");

            var agora = DateTime.UtcNow;
            return await _context.Agendamentos.AsNoTracking()
                .Where(a => a.PacienteId == pacienteId && a.DataHora <= agora)
                .OrderByDescending(a => a.DataHora)
                .ThenByDescending(a => a.Id)
                .Select(a => new HistoricoConsultaDTO
                {
                    AgendamentoId = a.Id,
                    DataHora = a.DataHora,
                    ProfissionalId = a.ProfissionalId,
                    ProfissionalNome = a.Profissional!.NomeCompleto,
                    Status = a.Status
                })
                .ToListAsync();
        }
    }
}
