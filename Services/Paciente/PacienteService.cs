using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; 
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.Entities;
using AgendaiFisio.Constants;
using AgendaiFisio.Context;

namespace AgendaiFisio.Services.Paciente
{
    // A interface IPacienteService também precisará ser atualizada para retornar Task<>
    public class PacienteService : IPacienteService
    {
        private readonly AgendaiFisioDbContext _context;

        public PacienteService(AgendaiFisioDbContext context)
        {
            _context = context;
        }

        public async Task<PacienteResponseDTO> GetPacienteByIdAsync(Guid id)
        {
            var paciente = await _context.Pacientes
                .Include(p => p.Usuario) 
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paciente == null) return null;

            return new PacienteResponseDTO
            {
                Id = paciente.Id,
                Nome = paciente.NomeCompleto,
                Telefone = paciente.Telefone,
                Email = paciente.Usuario?.Email 
            };
        }

        public async Task<PacienteResponseDTO> CreatePacienteAsync(PacienteCreateDTO paciente)
        {
            bool emailExists = await _context.Usuarios.AnyAsync(u => u.Email == paciente.Email);
            if (emailExists)
            {
                throw new Exception("O e-mail informado já está em uso.");
            }

            bool cpfExists = await _context.Pacientes.AnyAsync(p => p.Cpf == paciente.Cpf);
            if (cpfExists)
            {
                throw new Exception("O CPF informado já está em uso.");
            }

            var usuario = new Usuario
            {
                Email = paciente.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(paciente.Senha),
                TipoUsuario = PerfilDeUsuario.Paciente
            };
            
            // Apenas adicionamos ao contexto, ainda não vai para o banco
            _context.Usuarios.Add(usuario);

            var novoPaciente = new Entities.Paciente 
            {
                Usuario = usuario,
                NomeCompleto = paciente.Nome,
                Cpf = paciente.Cpf,
                Telefone = paciente.Telefone
            };
            
            _context.Pacientes.Add(novoPaciente);
            
            await _context.SaveChangesAsync();

            return new PacienteResponseDTO
            {
                Id = novoPaciente.Id,
                Nome = novoPaciente.NomeCompleto,
                Telefone = novoPaciente.Telefone,
                Email = usuario.Email
            };
        }

        public async Task<PacienteResponseDTO> UpdatePacienteAsync(Guid id, PacienteUpdateDTO paciente)
        {
            var existingPaciente = await _context.Pacientes
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingPaciente == null) return null;

            if (existingPaciente.Usuario != null && existingPaciente.Usuario.Email != paciente.Email)
            {
                bool emailInUse = await _context.Usuarios.AnyAsync(u => u.Email == paciente.Email && u.Id != existingPaciente.UsuarioId);
                if (emailInUse)
                {
                    throw new Exception("O novo e-mail informado já está em uso por outra conta.");
                }
                existingPaciente.Usuario.Email = paciente.Email;
            }

            existingPaciente.NomeCompleto = paciente.Nome;
            existingPaciente.Telefone = paciente.Telefone;
            
            await _context.SaveChangesAsync();

            return new PacienteResponseDTO
            {
                Id = existingPaciente.Id,
                Nome = existingPaciente.NomeCompleto,
                Telefone = existingPaciente.Telefone,
                Email = existingPaciente.Usuario?.Email
            };
        }
    }
}