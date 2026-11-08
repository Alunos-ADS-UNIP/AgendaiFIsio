namespace AgendaiFisio.Repositorio.Interfaces;

public interface IArmazenamentoFotoPerfil
{
    long TamanhoMaximoBytes { get; }

    Task<string> SalvarAsync(
        Guid profissionalId,
        Stream conteudo,
        string tipoConteudo,
        CancellationToken cancellationToken = default);

    Task ExcluirAsync(string chave, CancellationToken cancellationToken = default);

    string GerarUrlLeitura(string chave);
}
