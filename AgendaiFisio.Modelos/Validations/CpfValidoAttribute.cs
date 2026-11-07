using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.Validations;

public class CpfValidoAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        // Required trata os valores ausentes.
        if (value is null) return true;
        if (value is not string texto) return false;
        if (string.IsNullOrWhiteSpace(texto)) return true;
        var cpf = texto.Trim().Replace(".", "").Replace("-", "");
        if (cpf.Length != 11 || cpf.Any(c => c < '0' || c > '9') || cpf.All(c => c == cpf[0]))
            return false;
        for (var tamanho = 9; tamanho <= 10; tamanho++)
        {
            var soma = 0;
            for (var i = 0; i < tamanho; i++)
                soma += (cpf[i] - '0') * (tamanho + 1 - i);
            var resto = soma % 11;
            var digito = resto < 2 ? 0 : 11 - resto;
            if (cpf[tamanho] - '0' != digito) return false;
        }
        return true;
    }
}
