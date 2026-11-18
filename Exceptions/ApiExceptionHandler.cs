using FindMe.Dtos;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace FindMe.Exceptions;

/// <summary>
/// Converte exceções em respostas JSON padronizadas (equivalente ao GlobalExceptionHandler do projeto Java).
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        int status;
        string message;

        if (exception is ApiException api)
        {
            status = api.StatusCode;
            message = api.Message;
        }
        else
        {
            _logger.LogError(exception, "Erro não tratado");
            status = StatusCodes.Status500InternalServerError;
            message = "Erro interno inesperado";
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            new ErroResponse(DateTime.UtcNow, status, ReasonPhrases.GetReasonPhrase(status), message), ct);
        return true;
    }
}
