using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AgendaiFisio.Validations
{
    public class ProibirValoresAttribute : ValidationAttribute
    {
        private readonly string[] _valoresProibidos;

        public ProibirValoresAttribute(params string[] valoresProibidos)
        {
            _valoresProibidos = valoresProibidos;
        }

        
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true; 
     
            string textoDigitado = value.ToString()!.Trim().ToLower(); 
           
            foreach (var proibido in _valoresProibidos)
            {
                if (textoDigitado == proibido.ToLower())
                {
                    return false; 
                }
            }

            return true; 
        }
    }
}