namespace AgendaiFisio.DTOs.Profissional
{
    // Recebe os parâmetros de busca vindos da query string da listagem.
    public class ProfissionalFiltroDTO
    {
        public string? Nome { get; set; }
        public string? Especialidade { get; set; }
        public bool? Ativo { get; set; }

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
            set => _tamanhoPagina = value < 1 ? 10 : System.Math.Min(value, TamanhoPaginaMaximo);
        }
    }
}