using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Validations
{
    // Impede o uso de palavras escolhidas em um campo.
    public class ProibirValoresAttribute : ValidationAttribute
    {
        private readonly string[] _valoresProibidos;

        // Guarda os valores que não podem ser usados.
        public ProibirValoresAttribute(params string[] valoresProibidos)
        {
            _valoresProibidos = valoresProibidos;
        }

        // Verifica se o valor informado pode ser aceito.
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true;

            string textoDigitado = value.ToString()!.Trim().ToLower();

            foreach (var proibido in _valoresProibidos)
            {
                if (textoDigitado == proibido.ToLower())
                {
                    // Rejeita o texto quando ele está na lista proibida.
                    return false;
                }
            }

            return true;
        }
    }
}