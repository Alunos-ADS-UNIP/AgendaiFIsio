namespace AgendaiFisio.Constants;

// Contrato único usado pela API, pelos testes e pelo texto exibido no aplicativo.
public static class PoliticaDadosDemonstracao
{
    public const string VersaoTermosAtual = "2026-11-10";
    public const int PrazoRetencaoDias = 90;
    public const int IdadeConsentimentoResponsavel = 16;

    public static int CalcularIdade(DateTime dataNascimento, DateTime dataReferencia)
    {
        var nascimento = dataNascimento.Date;
        var referencia = dataReferencia.Date;
        var idade = referencia.Year - nascimento.Year;

        if (nascimento > referencia.AddYears(-idade))
            idade--;

        return idade;
    }
}
