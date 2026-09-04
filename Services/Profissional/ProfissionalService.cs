using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AgendaiFisio.Context;
using AgendaiFisio.DTOs.Profissional;
using AgendaiFisio.Entities;

namespace AgendaiFisio.Services.Profissional
{
    public class ProfissionalService : IProfissionalService
    {
        private readonly AgendaiFisioDbContext _context;

        public ProfissionalService(AgendaiFisioDbContext context)
        {
            _context = context;
        }

        public async Task<ProfissionalResponseDTO> GetProfissionalByIdAsync(Guid id)
        {
            var profissional = await _context.Profissionais
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profissional == null) return null;

            return new ProfissionalResponseDTO
            {
                Id = profissional.Id,
                NomeCompleto = profissional.NomeCompleto,
                Crefito = profissional.Crefito,
                Especialidade = profissional.Especialidade,
                Telefone = profissional.Telefone,
                Email = profissional.Usuario?.Email,
                Ativo = profissional.Ativo
            };
        }

        public async Task<ProfissionalResponseDTO> CreateProfissionalAsync(ProfissionalCreateDTO profissional)
        {
            // Validações de duplicidade
            bool emailExists = await _context.Usuarios.AnyAsync(u => u.Email == profissional.Email);
            if (emailExists) throw new Exception("O e-mail informado já está em uso.");

            bool cpfExists = await _context.Profissionais.AnyAsync(p => p.Cpf == profissional.Cpf);
            if (cpfExists) throw new Exception("O CPF informado já está em uso.");

            bool crefitoExists = await _context.Profissionais.AnyAsync(p => p.Crefito == profissional.Crefito);
            if (crefitoExists) throw new Exception("O CREFITO informado já está em uso.");

            // Criação da credencial de acesso
            var usuario = new Usuario
            {
                Email = profissional.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(profissional.Senha),
                TipoUsuario = "Profissional" // Pode usar sua constante/enum aqui
            };
            
            _context.Usuarios.Add(usuario);

            // Criação do perfil do Profissional
            var novoProfissional = new Entities.Profissional
            {
                Usuario = usuario,
                NomeCompleto = profissional.Nome, 
                Cpf = profissional.Cpf,
                Crefito = profissional.Crefito,
                Telefone = profissional.Telefone,
                Especialidade = profissional.Especialidade,
                DataNascimento = profissional.DataNascimento,
                DataCadastro = DateTime.UtcNow,
                
                // REGRA DE NEGÓCIO: O profissional entra inativo até ser aprovado pela administração
                Ativo = false 
            };
            
            _context.Profissionais.Add(novoProfissional);
            await _context.SaveChangesAsync();

            return new ProfissionalResponseDTO
            {
                Id = novoProfissional.Id,
                NomeCompleto = novoProfissional.NomeCompleto,
                Crefito = novoProfissional.Crefito,
                Especialidade = novoProfissional.Especialidade,
                Telefone = novoProfissional.Telefone,
                Email = usuario.Email,
                Ativo = novoProfissional.Ativo
            };
        }

        public async Task<ProfissionalResponseDTO> UpdateProfissionalAsync(Guid id, ProfissionalUpdateDTO profissional)
        {
            var existingProfissional = await _context.Profissionais
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProfissional == null) return null;

            if (existingProfissional.Usuario != null && existingProfissional.Usuario.Email != profissional.Email)
            {
                bool emailInUse = await _context.Usuarios.AnyAsync(u => u.Email == profissional.Email && u.Id != existingProfissional.UsuarioId);
                if (emailInUse) throw new Exception("O novo e-mail informado já está em uso por outra conta.");
                
                existingProfissional.Usuario.Email = profissional.Email;
            }

            existingProfissional.NomeCompleto = profissional.Nome; 
            existingProfissional.Telefone = profissional.Telefone;
            existingProfissional.Especialidade = profissional.Especialidade;
            
            await _context.SaveChangesAsync();

            return new ProfissionalResponseDTO
            {
                Id = existingProfissional.Id,
                NomeCompleto = existingProfissional.NomeCompleto,
                Crefito = existingProfissional.Crefito,
                Especialidade = existingProfissional.Especialidade,
                Telefone = existingProfissional.Telefone,
                Email = existingProfissional.Usuario?.Email,
                Ativo = existingProfissional.Ativo
            };
        }
    }
}