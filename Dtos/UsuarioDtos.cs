using System.ComponentModel.DataAnnotations;
using FindMe.Models;

namespace FindMe.Dtos;

public abstract class UsuarioRequestBase
{
    [Required(ErrorMessage = "nome é obrigatório")]
    [StringLength(100, ErrorMessage = "nome deve ter no máximo 100 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "email é obrigatório")]
    [EmailAddress(ErrorMessage = "email inválido")]
    [StringLength(255, ErrorMessage = "email deve ter no máximo 255 caracteres")]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^\+?[0-9()\-\s]{8,20}$", ErrorMessage = "telefone inválido")]
    public string? Telefone { get; set; }

    [Required(ErrorMessage = "tipoUsuario é obrigatório (RESPONSAVEL, COLABORADOR ou MODERADOR)")]
    public TipoUsuario? TipoUsuario { get; set; }
}

public class UsuarioCreateRequest : UsuarioRequestBase
{
    [Required(ErrorMessage = "senha é obrigatória")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "senha deve ter entre 8 e 72 caracteres")]
    public string Senha { get; set; } = string.Empty;
}

public class UsuarioUpdateRequest : UsuarioRequestBase
{
    /// <summary>No PUT é opcional: se vier nula, a senha atual é mantida.</summary>
    [StringLength(72, MinimumLength = 8, ErrorMessage = "senha deve ter entre 8 e 72 caracteres")]
    public string? Senha { get; set; }
}

public class UsuarioResponse
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public TipoUsuario TipoUsuario { get; set; }
    public DateTime DataCadastro { get; set; }

    public static UsuarioResponse From(Usuario u) => new()
    {
        Id = u.Id,
        Nome = u.Nome,
        Email = u.Email,
        Telefone = u.Telefone,
        TipoUsuario = u.TipoUsuario,
        DataCadastro = u.DataCadastro
    };
}
