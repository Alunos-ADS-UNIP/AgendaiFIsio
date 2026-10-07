using Microsoft.AspNetCore.Components.Forms;

namespace AgendaiFisio.DTOs.Agendamento;
using Validations;
using System.ComponentModel.DataAnnotations;


public class ReagendamentoDTO
{
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public DateOnly Data { get; set; }
    
    [Required]
    public TimeOnly Hora { get; set; }
    
    public ReagendamentoDTO()
    {
    }
}