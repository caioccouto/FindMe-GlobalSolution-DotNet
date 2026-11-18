namespace FindMe.Dtos;

/// <summary>Formato padrão de erro (404, 409, 500...). Mesmo formato da API Java.</summary>
public record ErroResponse(DateTime Timestamp, int Status, string Error, string Message);

public record ErroCampo(string Field, string Message);

/// <summary>Formato de erro de validação (400).</summary>
public record ValidationErrorResponse(DateTime Timestamp, int Status, string Error, IEnumerable<ErroCampo> Errors);
