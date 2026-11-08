using System.ComponentModel.DataAnnotations;

namespace AgendaiFisio.Validations;

/// Exige uma confirmação explícita no cadastro. Campo ausente é desserializado
/// como false e também é rejeitado.
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AceiteLgpdObrigatorioAttribute : ValidationAttribute
{
    public AceiteLgpdObrigatorioAttribute()
        : base("É necessário aceitar os Termos de Uso e a Política de Privacidade.")
    {
    }

    public override bool IsValid(object? value) => value is true;
}
