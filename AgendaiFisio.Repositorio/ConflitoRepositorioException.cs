namespace AgendaiFisio.Repositorio;

public sealed class ConflitoRepositorioException(
    string codigo,
    Exception innerException)
    : Exception("O banco rejeitou a operação por conflito de dados.", innerException)
{
    public string Codigo { get; } = codigo;
}
