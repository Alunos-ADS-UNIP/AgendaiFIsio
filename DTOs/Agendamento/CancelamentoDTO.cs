namespace AgendaiFisio.DTOs.Agendamento;
using System.ComponentModel.DataAnnotations;

public class CancelamentoDTO
{
    [Required] 
    public Guid Id {get;}
    public string Status="Cancelado";
    
}