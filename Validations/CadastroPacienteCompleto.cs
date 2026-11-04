using AgendaiFisio.Entities;

namespace AgendaiFisio.Validations;

// Um paciente só pode agendar depois de preencher os dados obrigatórios do cadastro.
// Enquanto o cadastro não está completo, o nome fica como "Cadastro Pendente".
public static class CadastroPacienteCompleto
{
    public const string NomePendente = "Cadastro Pendente";

    public static bool Verificar(Paciente paciente)
    {
        var endereco = paciente.Endereco;
        return !string.IsNullOrWhiteSpace(paciente.NomeCompleto)
            && !string.Equals(paciente.NomeCompleto.Trim(), NomePendente, StringComparison.OrdinalIgnoreCase)
            && paciente.Cpf.Count(char.IsDigit) == 11
            && paciente.DataNascimento.Year > 1900
            && !string.IsNullOrWhiteSpace(paciente.Telefone)
            && endereco is not null
            && !string.IsNullOrWhiteSpace(endereco.Rua)
            && !string.IsNullOrWhiteSpace(endereco.Numero)
            && !string.IsNullOrWhiteSpace(endereco.Cep)
            && !string.IsNullOrWhiteSpace(endereco.Bairro)
            && !string.IsNullOrWhiteSpace(endereco.Cidade)
            && !string.IsNullOrWhiteSpace(endereco.Estado);
    }
}

// Lançada quando o paciente tenta agendar sem o cadastro completo.
public sealed class CadastroIncompletoException(string mensagem) : Exception(mensagem);
