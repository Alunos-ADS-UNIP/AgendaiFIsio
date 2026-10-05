using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.Context;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.Entities;
using AgendaiFisio.Constants;
using AgendaiFisio.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgendaiFisio.Services.Paciente
{
    // Busca e atualiza os dados dos pacientes.
    public class PacienteService : IPacienteService
    {
        private readonly AgendaiFisioDbContext _context;
        private readonly ILogger<PacienteService> _logger;

        // Guarda o banco usado pelo serviço.
        public PacienteService(AgendaiFisioDbContext context, ILogger<PacienteService>? logger = null)
        {
            _context = context;
            _logger = logger ?? NullLogger<PacienteService>.Instance;
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
        public async Task<PagedResultDTO<HistoricoConsultaDTO>> ListarHistoricoConsultasAsync(
            Guid pacienteId, Guid usuarioId, string tipoUsuario, HistoricoConsultaFiltroDTO filtro)
        {
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
            {
                var pacienteExiste = await _context.Pacientes.AsNoTracking()
                    .AnyAsync(p => p.Id == pacienteId);
                _logger.LogInformation(
                    "Histórico não encontrado ou acesso negado. PacienteExiste={PacienteExiste}, TipoUsuario={TipoUsuario}",
                    pacienteExiste, tipoUsuario);
                throw new KeyNotFoundException("Histórico de consultas não encontrado.");
            }

            var agora = DateTime.UtcNow;
            var consulta = _context.Agendamentos.AsNoTracking()
                .Where(a => a.PacienteId == pacienteId && a.DataHora <= agora);
            var totalRegistros = await consulta.CountAsync();
            var deslocamento = ((long)filtro.Pagina - 1) * filtro.TamanhoPagina;

            var itens = deslocamento >= totalRegistros
                ? new List<HistoricoConsultaDTO>()
                : await consulta
                    .OrderByDescending(a => a.DataHora)
                    .ThenByDescending(a => a.Id)
                    .Skip((int)deslocamento)
                    .Take(filtro.TamanhoPagina)
                    .Select(a => new HistoricoConsultaDTO
                    {
                        AgendamentoId = a.Id,
                        DataHora = a.DataHora,
                        ProfissionalId = a.ProfissionalId,
                        ProfissionalNome = a.Profissional!.NomeCompleto,
                        Status = a.Status
                    })
                    .ToListAsync();

            // SQL Server datetime2 não conserva DateTime.Kind; os horários persistidos são UTC.
            foreach (var item in itens)
                item.DataHora = DateTime.SpecifyKind(item.DataHora, DateTimeKind.Utc);

            return new PagedResultDTO<HistoricoConsultaDTO>
            {
                Itens = itens,
                TotalRegistros = totalRegistros,
                PaginaAtual = filtro.Pagina,
                TamanhoPagina = filtro.TamanhoPagina
            };
        }
    }
}
