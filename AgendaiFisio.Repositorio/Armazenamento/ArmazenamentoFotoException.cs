namespace AgendaiFisio.Repositorio.Armazenamento;

public sealed class ArmazenamentoFotoException : Exception
{
    public ArmazenamentoFotoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
