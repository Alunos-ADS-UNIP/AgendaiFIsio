namespace AgendaiFisio.DTOs.Paciente;

// Dados mínimos para identificar um paciente nos resultados da busca.
public sealed class PacienteListItemDTO
{
    public Guid Id { get; init; }
    public string NomeCompleto { get; init; } = string.Empty;
    public string CpfMascarado { get; init; } = string.Empty;
    public string Telefone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
