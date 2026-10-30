using System;

namespace AgendaiFisio.DTOs.Profissional
{
    // Perfil do próprio fisioterapeuta logado: o detalhe público mais CPF e data de nascimento,
    // que só ele pode consultar (para preencher a edição do perfil).
    public class ProfissionalMeDTO : ProfissionalDetailDTO
    {
        public string Cpf { get; set; } = string.Empty;

        // Null enquanto o cadastro ainda não foi completado.
        public DateTime? DataNascimento { get; set; }
    }
}
