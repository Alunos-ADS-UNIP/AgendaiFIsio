using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.Context;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.Entities;

namespace AgendaiFisio.Services.Paciente
{
    public class PacienteService : IPacienteService
    {
        private readonly AgendaiFisioDbContext _context;

        public PacienteService(AgendaiFisioDbContext context)
        {
            _context = context;
        }

        // 1. Método para buscar o paciente (exigido pela interface)
        public async Task<Entities.Paciente> GetPacienteByIdAsync(Guid id)
        {
            return await _context.Pacientes
                .Include(p => p.Endereco)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // 2. Método de atualização que criamos (agora com o nome correto)
        public async Task<bool> UpdatePacienteAsync(Guid usuarioId, PacienteUpdateDTO dto)
        {
            var paciente = await _context.Pacientes
                .Include(p => p.Endereco)
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);

            if (paciente == null)
                throw new Exception("Perfil de paciente não encontrado para este usuário.");

            paciente.NomeCompleto = dto.NomeCompleto;
            paciente.Cpf = dto.Cpf;
            paciente.DataNascimento = dto.DataNascimento;
            paciente.Telefone = dto.Telefone;
            paciente.Sexo = dto.Sexo;
            paciente.EstadoCivil = dto.EstadoCivil;

            if (paciente.Endereco == null)
            {
                paciente.Endereco = new Endereco();
            }

            // Atualiza os dados do endereço vindos do DTO
            paciente.Endereco.Rua = dto.Rua;
            paciente.Endereco.Numero = dto.Numero;
            paciente.Endereco.Cep = dto.Cep;
            
            // Preenche com vazio caso o DTO não tenha essas propriedades, 
            // evitando o erro de "Cannot insert NULL" no banco de dados
            paciente.Endereco.Complemento = string.Empty;
            paciente.Endereco.Bairro = string.Empty;
            paciente.Endereco.Cidade = string.Empty;
            paciente.Endereco.Estado = string.Empty;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}