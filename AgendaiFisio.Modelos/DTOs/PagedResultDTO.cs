using System.Collections.Generic;

namespace AgendaiFisio.DTOs
{
    // Envelope genérico para respostas paginadas de qualquer listagem.
    public class PagedResultDTO<T>
    {
        public List<T> Itens { get; set; } = new();
        public int TotalRegistros { get; set; }
        public int PaginaAtual { get; set; }
        public int TamanhoPagina { get; set; }
        public int TotalPaginas => TamanhoPagina > 0
            ? (int)System.Math.Ceiling(TotalRegistros / (double)TamanhoPagina)
            : 0;
    }
}