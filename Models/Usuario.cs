namespace FindMe.Models;

public class Usuario
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash BCrypt da senha (nunca a senha em texto puro, nunca devolvida pela API).</summary>
    public string Senha { get; set; } = string.Empty;

    public string? Telefone { get; set; }
    public TipoUsuario TipoUsuario { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    /// <summary>Relacionamento 1:N - um usuário (responsável) tem vários casos.</summary>
    public ICollection<Caso> Casos { get; set; } = new List<Caso>();
}
