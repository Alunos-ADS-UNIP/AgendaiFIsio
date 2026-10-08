namespace AgendaiFisio.DTOs.Paciente;

// Perfil completo devolvido somente ao próprio paciente autenticado.
public sealed class PacienteMeResponseDTO
{
    public Guid Id { get; init; }
    public Guid UsuarioId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string NomeCompleto { get; init; } = string.Empty;
    public string Cpf { get; init; } = string.Empty;
    public string Telefone { get; init; } = string.Empty;
    public DateTime DataNascimento { get; init; }
    public string Sexo { get; init; } = string.Empty;
    public string EstadoCivil { get; init; } = string.Empty;
    public EnderecoPacienteDTO Endereco { get; init; } = new();
}

public sealed class EnderecoPacienteDTO
{
    public string Rua { get; init; } = string.Empty;
    public string Numero { get; init; } = string.Empty;
    public string Complemento { get; init; } = string.Empty;
    public string Cep { get; init; } = string.Empty;
    public string Bairro { get; init; } = string.Empty;
    public string Cidade { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
}
