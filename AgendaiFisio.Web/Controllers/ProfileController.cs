using AgendaiFisio.DTOs.Especialidade;
using AgendaiFisio.DTOs.Paciente;
using AgendaiFisio.DTOs.Profissional;
using AgendaiFisio.Web.Models;
using AgendaiFisio.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Web.Controllers;

[Authorize(Roles = "Paciente,Profissional")]
[Route("perfil")]
public sealed class ProfileController : PortalController
{
    private readonly IAgendaiFisioApiClient _api;

    public ProfileController(IAgendaiFisioApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            if (User.IsInRole("Profissional"))
                return View("Professional", await BuildProfessionalModelAsync(cancellationToken));

            var patient = await _api.GetAsync<PacienteMeResponseDTO>("/api/paciente/me", true, cancellationToken);
            return View("Patient", ToPatientModel(patient));
        }
        catch (ApiRequestException exception)
        {
            return await HandleReadErrorAsync(exception);
        }
    }

    [Authorize(Roles = "Paciente")]
    [HttpPost("paciente")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePatient(PatientProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("Patient", model);
        try
        {
            await _api.PutAsync(
                "/api/paciente/completar-perfil",
                new PacienteUpdateDTO
                {
                    NomeCompleto = model.FullName.Trim(),
                    Cpf = Digits(model.Cpf),
                    Telefone = Digits(model.Phone),
                    DataNascimento = model.BirthDate,
                    Sexo = model.Gender,
                    EstadoCivil = model.MaritalStatus,
                    Rua = model.Street.Trim(),
                    Numero = model.Number.Trim(),
                    Complemento = model.Complement?.Trim(),
                    Cep = Digits(model.ZipCode),
                    Bairro = model.Neighborhood.Trim(),
                    Cidade = model.City.Trim(),
                    Estado = model.State.Trim().ToUpperInvariant()
                },
                cancellationToken);
            TempData["Success"] = "Perfil atualizado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiRequestException exception)
        {
            AddApiErrors(exception);
            return View("Patient", model);
        }
    }

    [Authorize(Roles = "Profissional")]
    [HttpPost("profissional")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfessional(ProfessionalProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadSpecialtiesAsync(model, cancellationToken);
            return View("Professional", model);
        }

        try
        {
            await _api.PutAsync(
                "/api/profissional/completar-perfil",
                new ProfissionalUpdateDTO
                {
                    NomeCompleto = model.FullName.Trim(),
                    Cpf = Digits(model.Cpf),
                    Crefito = model.Crefito.Trim(),
                    Telefone = Digits(model.Phone),
                    DataNascimento = model.BirthDate,
                    Bio = model.Bio.Trim(),
                    Rua = model.Street.Trim(),
                    Numero = model.Number.Trim(),
                    Complemento = model.Complement.Trim(),
                    Cep = Digits(model.ZipCode),
                    Bairro = model.Neighborhood.Trim(),
                    Cidade = model.City.Trim(),
                    Estado = model.State.Trim().ToUpperInvariant(),
                    Ativo = model.IsActive
                },
                cancellationToken);

            if (model.SpecialtyId is Guid specialtyId)
            {
                await _api.PutAsync(
                    "/api/profissional/especialidade",
                    new ProfissionalEspecialidadeUpdateDTO { EspecialidadeId = specialtyId },
                    cancellationToken);
            }

            TempData["Success"] = "Perfil profissional atualizado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiRequestException exception)
        {
            AddApiErrors(exception);
            await LoadSpecialtiesAsync(model, cancellationToken);
            return View("Professional", model);
        }
    }

    [Authorize(Roles = "Profissional")]
    [HttpPost("foto")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(IFormFile? photo, CancellationToken cancellationToken)
    {
        if (photo is null || photo.Length == 0)
        {
            TempData["Error"] = "Escolha uma imagem para enviar.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _api.UploadAsync<FotoPerfilResponseDTO>("/api/profissional/me/foto", photo, cancellationToken);
            TempData["Success"] = "Foto de perfil atualizada.";
        }
        catch (ApiRequestException exception) { TempData["Error"] = exception.Message; }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Profissional")]
    [HttpPost("foto/remover")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(CancellationToken cancellationToken)
    {
        try
        {
            await _api.DeleteAsync("/api/profissional/me/foto", cancellationToken);
            TempData["Success"] = "Foto removida.";
        }
        catch (ApiRequestException exception) { TempData["Error"] = exception.Message; }
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfessionalProfileViewModel> BuildProfessionalModelAsync(CancellationToken cancellationToken)
    {
        var profileTask = _api.GetAsync<ProfissionalMeDTO>("/api/profissional/me", true, cancellationToken);
        var specialtiesTask = _api.GetAsync<List<EspecialidadeResponseDTO>>("/api/especialidade", false, cancellationToken);
        await Task.WhenAll(profileTask, specialtiesTask);
        var profile = profileTask.Result;
        return new ProfessionalProfileViewModel
        {
            FullName = PendingToEmpty(profile.NomeCompleto),
            Cpf = profile.Cpf,
            Crefito = profile.Crefito,
            Phone = profile.Telefone,
            BirthDate = profile.DataNascimento ?? default,
            Bio = profile.Bio,
            SpecialtyId = profile.Especialidade?.Id,
            Email = profile.Email,
            PhotoUrl = profile.FotoPerfilUrl,
            IsActive = profile.Ativo,
            Street = profile.Endereco?.Rua ?? string.Empty,
            Number = profile.Endereco?.Numero ?? string.Empty,
            Complement = profile.Endereco?.Complemento ?? string.Empty,
            ZipCode = profile.Endereco?.Cep ?? string.Empty,
            Neighborhood = profile.Endereco?.Bairro ?? string.Empty,
            City = profile.Endereco?.Cidade ?? string.Empty,
            State = profile.Endereco?.Estado ?? string.Empty,
            Specialties = specialtiesTask.Result
        };
    }

    private async Task LoadSpecialtiesAsync(ProfessionalProfileViewModel model, CancellationToken cancellationToken)
    {
        try { model.Specialties = await _api.GetAsync<List<EspecialidadeResponseDTO>>("/api/especialidade", false, cancellationToken); }
        catch { model.Specialties = []; }
    }

    private static PatientProfileViewModel ToPatientModel(PacienteMeResponseDTO patient) => new()
    {
        FullName = PendingToEmpty(patient.NomeCompleto),
        Cpf = patient.Cpf,
        Phone = patient.Telefone,
        BirthDate = patient.DataNascimento,
        Gender = patient.Sexo,
        MaritalStatus = patient.EstadoCivil,
        Email = patient.Email,
        Street = patient.Endereco.Rua,
        Number = patient.Endereco.Numero,
        Complement = patient.Endereco.Complemento,
        ZipCode = patient.Endereco.Cep,
        Neighborhood = patient.Endereco.Bairro,
        City = patient.Endereco.Cidade,
        State = patient.Endereco.Estado
    };

    private static string PendingToEmpty(string value) =>
        value.Equals("Cadastro Pendente", StringComparison.OrdinalIgnoreCase) ? string.Empty : value;

    private static string Digits(string value) => new(value.Where(char.IsDigit).ToArray());
}
