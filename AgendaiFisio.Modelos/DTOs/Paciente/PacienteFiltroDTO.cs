namespace AgendaiFisio.DTOs.Paciente;

// Parâmetros de pesquisa e paginação da listagem exclusiva para profissionais.
public sealed class PacienteFiltroDTO
{
    // Pesquisa simultaneamente por nome e CPF. O CPF pode ser informado com ou sem pontuação.
    public string? Busca { get; set; }

    public string? Nome { get; set; }
    public string? Documento { get; set; }

    private const int TamanhoPaginaMaximo = 50;
    private int _pagina = 1;
    private int _tamanhoPagina = 10;

    public int Pagina
    {
        get => _pagina;
        set => _pagina = value < 1 ? 1 : value;
    }

    public int TamanhoPagina
    {
        get => _tamanhoPagina;
        set => _tamanhoPagina = value < 1 ? 10 : Math.Min(value, TamanhoPaginaMaximo);
    }
}
