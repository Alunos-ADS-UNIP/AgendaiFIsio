using System.ComponentModel.DataAnnotations;
using AgendaiFisio.Constants;

namespace AgendaiFisio.Validations;

public sealed class TipoUsuarioValidoAttribute : ValidationAttribute
{
    public TipoUsuarioValidoAttribute() : base("Tipo de usuário inválido. Use Paciente ou Profissional.") { }
    public override bool IsValid(object? value) =>
        value is string texto && PerfilDeUsuario.NormalizarCadastro(texto) is not null;
}
