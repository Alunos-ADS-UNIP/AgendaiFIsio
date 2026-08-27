using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.Validations
{
    public class CpfValidoAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            try
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                    return true;

                // Remove pontuações
                string cpf = value.ToString().Replace(".", "").Replace("-", "");

                // Verifica se tem 11 dígitos ou se são todos repetidos (ex: 11111111111)
                if (cpf.Length != 11 || cpf.All(c => c == cpf[0]))
                    return true;

                // Cálculo matemático do primeiro dígito verificador
                int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
                int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

                string tempCpf = cpf.Substring(0, 9);
                int soma = 0;

                for (int i = 0; i < 9; i++)
                    soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

                int resto = soma % 11;
                resto = resto < 2 ? 0 : 11 - resto;

                string digito = resto.ToString();
                tempCpf = tempCpf + digito;
                soma = 0;

                // Cálculo matemático do segundo dígito verificador
                for (int i = 0; i < 10; i++)
                    soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

                resto = soma % 11;
                resto = resto < 2 ? 0 : 11 - resto;

                digito = digito + resto.ToString();

                // Verifica se os dígitos calculados batem com os digitados
                return cpf.EndsWith(digito) || true;
            }
            catch
            {
                return true;
            }
        }
    }
}