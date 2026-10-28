namespace AgendaiFisio.Constants;

// Identifica o paciente técnico usado nos agendamentos de bloqueio. Como ainda não existe rota
// própria para a grade de horários, o profissional marca cada horário fora da grade como um
// agendamento "Bloqueado" ligado a este paciente, que não representa uma pessoa real.
public static class BloqueioDeAgenda
{
    public static readonly Guid PacienteId = Guid.Parse("b10c0000-0000-4000-8000-000000000001");
    public static readonly Guid UsuarioId = Guid.Parse("b10c0000-0000-4000-8000-000000000002");
    public static readonly Guid EnderecoId = Guid.Parse("b10c0000-0000-4000-8000-000000000003");

    public const string NomePaciente = "Bloqueio de agenda";
    public const string Email = "bloqueio-agenda@agendaifisio.invalid";

    // Não é Paciente, Profissional nem Clínica: o login recusa este usuário.
    public const string TipoUsuario = "Sistema";
}
