using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Constants
{
    // Reúne os nomes dos tipos de usuário aceitos pelo sistema.
    public static class PerfilDeUsuario
    {
        public static string? NormalizarCadastro(string? valor)
        {
            if (string.Equals(valor?.Trim(), Paciente, StringComparison.OrdinalIgnoreCase)) return Paciente;
            if (string.Equals(valor?.Trim(), Profissional, StringComparison.OrdinalIgnoreCase)) return Profissional;
            return null;
        }

        public static string? NormalizarAutenticado(string? valor)
        {
            if (string.Equals(valor?.Trim(), Admin, StringComparison.OrdinalIgnoreCase)) return Admin;
            return NormalizarCadastro(valor);
        }

        public const string Admin = "Clinica";
        public const string Paciente = "Paciente";
        public const string Profissional = "Profissional";
    }
}
