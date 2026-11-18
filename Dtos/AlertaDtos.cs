using System.ComponentModel.DataAnnotations;
using FindMe.Models;

namespace FindMe.Dtos;

public class AlertaUpdateRequest
{
    [Required(ErrorMessage = "titulo é obrigatório")]
    [StringLength(150, ErrorMessage = "titulo deve ter no máximo 150 caracteres")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "descricaoPublica é obrigatória")]
    [StringLength(1000, ErrorMessage = "descricaoPublica deve ter no máximo 1000 caracteres")]
    public string DescricaoPublica { get; set; } = string.Empty;
}

public class AlertaCreateRequest : AlertaUpdateRequest
{
    [Required(ErrorMessage = "casoId é obrigatório")]
    [Range(1, long.MaxValue, ErrorMessage = "casoId deve ser positivo")]
    public long? CasoId { get; set; }
}

/// <summary>
/// Privacidade: o alerta público NÃO expõe responsável, telefone, descrição privada
/// nem a última localização conhecida (igual à versão Java).
/// </summary>
public class AlertaResponse
{
    public long Id { get; set; }
    public long CasoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string DescricaoPublica { get; set; } = string.Empty;
    public DateTime DataPublicacao { get; set; }
    public bool Ativo { get; set; }
    public string NomeDesaparecido { get; set; } = string.Empty;
    public int Idade { get; set; }
    public DateOnly DataDesaparecimento { get; set; }

    public static AlertaResponse From(Alerta a) => new()
    {
        Id = a.Id,
        CasoId = a.CasoId,
        Titulo = a.Titulo,
        DescricaoPublica = a.DescricaoPublica,
        DataPublicacao = a.DataPublicacao,
        Ativo = a.Ativo,
        NomeDesaparecido = a.Caso.NomeDesaparecido,
        Idade = a.Caso.Idade,
        DataDesaparecimento = DateOnly.FromDateTime(a.Caso.DataDesaparecimento)
    };
}
