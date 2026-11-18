namespace FindMe.Exceptions;

/// <summary>Base das exceções de negócio. Cada uma sabe qual status HTTP representa.</summary>
public abstract class ApiException : Exception
{
    protected ApiException(string message) : base(message) { }
    public abstract int StatusCode { get; }
}

/// <summary>404 - id inexistente.</summary>
public class RecursoNaoEncontradoException : ApiException
{
    public RecursoNaoEncontradoException(string recurso, long id)
        : base($"{recurso} com id {id} não encontrado(a)") { }
    public override int StatusCode => StatusCodes.Status404NotFound;
}

/// <summary>409 - violação de regra de negócio (transição de status inválida, e-mail duplicado, etc.).</summary>
public class ConflitoException : ApiException
{
    public ConflitoException(string message) : base(message) { }
    public override int StatusCode => StatusCodes.Status409Conflict;
}

/// <summary>400 - requisição inválida que não é erro de formato do corpo.</summary>
public class RequisicaoInvalidaException : ApiException
{
    public RequisicaoInvalidaException(string message) : base(message) { }
    public override int StatusCode => StatusCodes.Status400BadRequest;
}
