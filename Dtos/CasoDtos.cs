using System.ComponentModel.DataAnnotations;
using FindMe.Models;
using FindMe.Validation;

namespace FindMe.Dtos;

/// <summary>Corpo do PUT /api/casos/{id}.</summary>
public class CasoUpdateRequest
{
    [Required(ErrorMessage = "nomeDesaparecido é obrigatório")]
    [StringLength(150, ErrorMessage = "nomeDesaparecido deve ter no máximo 150 caracteres")]
    public string NomeDesaparecido { get; set; } = string.Empty;

    [Required(ErrorMessage = "idade é obrigatória")]
    [Range(0, 17, ErrorMessage = "idade deve estar entre 0 e 17")]
    public int? Idade { get; set; }

    [Required(ErrorMessage = "descricao é obrigatória")]
    [StringLength(2000, ErrorMessage = "descricao deve ter no máximo 2000 caracteres")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "ultimaLocalizacaoConhecida é obrigatória")]
    [StringLength(300, ErrorMessage = "ultimaLocalizacaoConhecida deve ter no máximo 300 caracteres")]
    public string UltimaLocalizacaoConhecida { get; set; } = string.Empty;

    [Required(ErrorMessage = "dataDesaparecimento é obrigatória")]
    [DataNaoFutura(ErrorMessage = "dataDesaparecimento não pode estar no futuro")]
    public DateOnly? DataDesaparecimento { get; set; }
}

/// <summary>Corpo do POST /api/casos. Sem autenticação, o responsável é informado no corpo.</summary>
public class CasoCreateRequest : CasoUpdateRequest
{
    [Required(ErrorMessage = "responsavelId é obrigatório")]
    [Range(1, long.MaxValue, ErrorMessage = "responsavelId deve ser positivo")]
    public long? ResponsavelId { get; set; }
}

public class AlterarStatusRequest
{
    [Required(ErrorMessage = "status é obrigatório")]
    public StatusCaso? Status { get; set; }
}

public class CasoResponse
{
    public long Id { get; set; }
    public string NomeDesaparecido { get; set; } = string.Empty;
    public int Idade { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string UltimaLocalizacaoConhecida { get; set; } = string.Empty;
    public DateOnly DataDesaparecimento { get; set; }
    public StatusCaso Status { get; set; }
    public long ResponsavelId { get; set; }

    public static CasoResponse From(Caso c) => new()
    {
        Id = c.Id,
        NomeDesaparecido = c.NomeDesaparecido,
        Idade = c.Idade,
        Descricao = c.Descricao,
        UltimaLocalizacaoConhecida = c.UltimaLocalizacaoConhecida,
        DataDesaparecimento = DateOnly.FromDateTime(c.DataDesaparecimento),
        Status = c.Status,
        ResponsavelId = c.ResponsavelId
    };
}

public record StatusTotalResponse(StatusCaso Status, int Total);
