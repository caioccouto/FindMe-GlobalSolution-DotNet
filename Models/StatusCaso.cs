namespace FindMe.Models;

public enum StatusCaso
{
    EM_ANALISE,
    VALIDADO,
    PUBLICADO,
    EM_ACOMPANHAMENTO,
    LOCALIZADO,
    ENCERRADO
}

public static class StatusCasoExtensions
{
    /// <summary>Ciclo: EM_ANALISE → VALIDADO → PUBLICADO → EM_ACOMPANHAMENTO → LOCALIZADO → ENCERRADO.</summary>
    public static IReadOnlyCollection<StatusCaso> ProximosPermitidos(this StatusCaso status) => status switch
    {
        StatusCaso.EM_ANALISE => new[] { StatusCaso.VALIDADO, StatusCaso.ENCERRADO },
        StatusCaso.VALIDADO => new[] { StatusCaso.PUBLICADO, StatusCaso.ENCERRADO },
        StatusCaso.PUBLICADO => new[] { StatusCaso.EM_ACOMPANHAMENTO, StatusCaso.LOCALIZADO, StatusCaso.ENCERRADO },
        StatusCaso.EM_ACOMPANHAMENTO => new[] { StatusCaso.LOCALIZADO, StatusCaso.ENCERRADO },
        StatusCaso.LOCALIZADO => new[] { StatusCaso.ENCERRADO },
        _ => Array.Empty<StatusCaso>()
    };

    public static bool PodeTransitarPara(this StatusCaso status, StatusCaso destino) =>
        status.ProximosPermitidos().Contains(destino);
}
