using System;

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
            => NormalizarCadastro(valor);

        public const string Paciente = "Paciente";
        public const string Profissional = "Profissional";
        public const string AutenticadosMvp = Paciente + "," + Profissional;
    }
}
