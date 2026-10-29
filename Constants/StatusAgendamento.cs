namespace AgendaiFisio.Constants;

// Centraliza os estados persistidos para evitar divergências entre as regras de agenda.
public static class StatusAgendamento
{
    public const string Agendado = "Agendado";
    public const string Confirmado = "Confirmado";
    public const string Cancelado = "Cancelado";
    public const string Concluido = "Concluido";
}
