using System.Text.RegularExpressions;

namespace AgendaiFisio.Services.Especialidade
{
    // Aplica a regra de comparação combinada do catálogo: aparar as pontas do nome e reduzir
    // espaços internos seguidos a um só. Maiúsculas/minúsculas e acentos são tratados pela
    // collation ICU case_accent_insensitive da coluna Nome, não aqui.
    public static class EspecialidadeNomeNormalizador
    {
        public static string Normalizar(string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return string.Empty;
            }

            return Regex.Replace(nome.Trim(), @"\s+", " ");
        }
    }
}
