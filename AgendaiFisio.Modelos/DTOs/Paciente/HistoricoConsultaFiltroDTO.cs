namespace AgendaiFisio.DTOs.Paciente;

// Limita o volume devolvido em cada consulta ao histórico.
public class HistoricoConsultaFiltroDTO
{
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
