using System.ComponentModel.DataAnnotations;
using AgendaiFisio.DTOs;
using AgendaiFisio.DTOs.Agendamento;
using AgendaiFisio.DTOs.BloqueioAgenda;
using AgendaiFisio.DTOs.Especialidade;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.DTOs.Profissional;

namespace AgendaiFisio.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Digite um e-mail válido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha.")]
    [MinLength(6, ErrorMessage = "A senha precisa ter pelo menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel : IValidatableObject
{
    [Required, EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "As senhas não coincidem.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar senha")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Quero usar o AgendaiFisio como")]
    public string UserType { get; set; } = "Paciente";

    [Display(Name = "Data de nascimento")]
    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    [Display(Name = "Sou responsável legal pelo paciente")]
    public bool LegalGuardianConsent { get; set; }

    [Display(Name = "Nome do responsável legal")]
    public string? LegalGuardianName { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Você precisa aceitar os termos para continuar.")]
    [Display(Name = "Li e aceito os Termos de Uso e a Política de Privacidade")]
    public bool AcceptTerms { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UserType == "Paciente" && BirthDate is null)
            yield return new ValidationResult("Informe a data de nascimento do paciente.", new[] { nameof(BirthDate) });

        if (BirthDate is { } birthDate && birthDate.Date > DateTime.Today)
            yield return new ValidationResult("A data de nascimento não pode estar no futuro.", new[] { nameof(BirthDate) });

        if (UserType == "Paciente" && BirthDate is { } patientBirthDate && CalculateAge(patientBirthDate) < 16)
        {
            if (!LegalGuardianConsent)
                yield return new ValidationResult("Pacientes menores de 16 anos precisam da autorização do responsável legal.", new[] { nameof(LegalGuardianConsent) });
            if (string.IsNullOrWhiteSpace(LegalGuardianName))
                yield return new ValidationResult("Informe o nome do responsável legal.", new[] { nameof(LegalGuardianName) });
        }
    }

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}

public sealed class HomeViewModel
{
    public IReadOnlyList<EspecialidadeResponseDTO> Specialties { get; init; } = [];
}

public sealed class DashboardViewModel
{
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public IReadOnlyList<AgendamentoResponseDTO> Appointments { get; init; } = [];
    public IReadOnlyList<ProfissionalListItemDTO> Professionals { get; init; } = [];
    public IReadOnlyList<EspecialidadeResponseDTO> Specialties { get; init; } = [];
    public int TodayCount { get; init; }
}

public sealed class ProfessionalDirectoryViewModel
{
    public string? Search { get; init; }
    public Guid? SpecialtyId { get; init; }
    public PagedResultDTO<ProfissionalListItemDTO> Results { get; init; } = new();
    public IReadOnlyList<EspecialidadeResponseDTO> Specialties { get; init; } = [];
}

public sealed class AppointmentListViewModel
{
    public IReadOnlyList<AgendamentoResponseDTO> Appointments { get; init; } = [];
    public string? Status { get; init; }
    public DateTime? Date { get; init; }
}

public sealed class AppointmentCreateViewModel
{
    [Required(ErrorMessage = "Escolha um fisioterapeuta.")]
    [Display(Name = "Fisioterapeuta")]
    public Guid ProfessionalId { get; set; }

    [Required(ErrorMessage = "Escolha uma data e um horário.")]
    [Display(Name = "Data e horário")]
    public DateTime LocalDateTime { get; set; }

    [StringLength(1000)]
    [Display(Name = "Observações (opcional)")]
    public string? Notes { get; set; }

    public IReadOnlyList<ProfissionalListItemDTO> Professionals { get; set; } = [];
}

public sealed class RescheduleViewModel
{
    public Guid AppointmentId { get; set; }

    [Required(ErrorMessage = "Escolha a nova data e horário.")]
    [Display(Name = "Nova data e horário")]
    public DateTime LocalDateTime { get; set; }
}

public sealed class PatientProfileViewModel
{
    [Required, Display(Name = "Nome completo")]
    public string FullName { get; set; } = string.Empty;
    [Required, Display(Name = "CPF")]
    public string Cpf { get; set; } = string.Empty;
    [Required, Display(Name = "Telefone")]
    public string Phone { get; set; } = string.Empty;
    [Required, DataType(DataType.Date), Display(Name = "Data de nascimento")]
    public DateTime BirthDate { get; set; }
    [Display(Name = "Sexo")]
    public string Gender { get; set; } = string.Empty;
    [Display(Name = "Estado civil")]
    public string MaritalStatus { get; set; } = string.Empty;
    [Required, Display(Name = "CEP")]
    public string ZipCode { get; set; } = string.Empty;
    [Required, Display(Name = "Rua")]
    public string Street { get; set; } = string.Empty;
    [Required, Display(Name = "Número")]
    public string Number { get; set; } = string.Empty;
    [Display(Name = "Complemento")]
    public string? Complement { get; set; }
    [Required, Display(Name = "Bairro")]
    public string Neighborhood { get; set; } = string.Empty;
    [Required, Display(Name = "Cidade")]
    public string City { get; set; } = string.Empty;
    [Required, StringLength(2, MinimumLength = 2), Display(Name = "UF")]
    public string State { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class ProfessionalProfileViewModel
{
    [Required, Display(Name = "Nome completo")]
    public string FullName { get; set; } = string.Empty;
    [Required, Display(Name = "CPF")]
    public string Cpf { get; set; } = string.Empty;
    [Required, Display(Name = "CREFITO")]
    public string Crefito { get; set; } = string.Empty;
    [Required, Display(Name = "Telefone")]
    public string Phone { get; set; } = string.Empty;
    [Required, DataType(DataType.Date), Display(Name = "Data de nascimento")]
    public DateTime BirthDate { get; set; }
    [StringLength(1000), Display(Name = "Bio")]
    public string Bio { get; set; } = string.Empty;
    [Required, Display(Name = "Especialidade")]
    public Guid? SpecialtyId { get; set; }
    [Required, Display(Name = "CEP")]
    public string ZipCode { get; set; } = string.Empty;
    [Required, Display(Name = "Rua")]
    public string Street { get; set; } = string.Empty;
    [Required, Display(Name = "Número")]
    public string Number { get; set; } = string.Empty;
    [Display(Name = "Complemento")]
    public string Complement { get; set; } = string.Empty;
    [Required, Display(Name = "Bairro")]
    public string Neighborhood { get; set; } = string.Empty;
    [Required, Display(Name = "Cidade")]
    public string City { get; set; } = string.Empty;
    [Required, StringLength(2, MinimumLength = 2), Display(Name = "UF")]
    public string State { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Email { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public IReadOnlyList<EspecialidadeResponseDTO> Specialties { get; set; } = [];
}

public sealed class HistoryViewModel
{
    public PagedResultDTO<HistoricoConsultaDTO> History { get; init; } = new();
    public int Page { get; init; } = 1;
}

public sealed class PatientDirectoryViewModel
{
    public string? Search { get; init; }
    public PagedResultDTO<PacienteListItemDTO> Results { get; init; } = new();
}

public sealed class PatientDetailsViewModel
{
    public PacienteMeResponseDTO Patient { get; init; } = new();
    public PagedResultDTO<HistoricoConsultaDTO> History { get; init; } = new();
}

public sealed class AvailabilityViewModel
{
    public IReadOnlyList<BloqueioAgendaResponseDTO> Blocks { get; init; } = [];
    [DataType(DataType.Date)]
    public DateTime Day { get; set; } = DateTime.Today.AddDays(1);
    [DataType(DataType.DateTime)]
    public DateTime LocalDateTime { get; set; } = DateTime.Today.AddDays(1).AddHours(9);
    [StringLength(500)]
    public string? Reason { get; set; }
}
