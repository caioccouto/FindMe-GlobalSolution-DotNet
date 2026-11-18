using FindMe.Exceptions;

namespace FindMe.Models;

public class Caso
{
    public long Id { get; set; }
    public string NomeDesaparecido { get; set; } = string.Empty;
    public int Idade { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string UltimaLocalizacaoConhecida { get; set; } = string.Empty;

    /// <summary>Apenas a data (coluna DATE no Oracle); a hora é ignorada.</summary>
    public DateTime DataDesaparecimento { get; set; }

    /// <summary>Todo caso nasce EM_ANALISE. Só muda via <see cref="AlterarStatus"/>.</summary>
    public StatusCaso Status { get; private set; } = StatusCaso.EM_ANALISE;

    public long ResponsavelId { get; set; }
    public Usuario Responsavel { get; set; } = null!;

    /// <summary>Relacionamento 1:1 - um caso gera no máximo um alerta público.</summary>
    public Alerta? Alerta { get; set; }

    public void AlterarStatus(StatusCaso novo)
    {
        if (!Status.PodeTransitarPara(novo))
        {
            throw new ConflitoException(
                $"Transição inválida de {Status} para {novo}. Permitidas: {string.Join(", ", Status.ProximosPermitidos())}");
        }
        Status = novo;
    }

    public void RetirarPublicacao()
    {
        if (Status != StatusCaso.PUBLICADO && Status != StatusCaso.EM_ACOMPANHAMENTO)
        {
            throw new ConflitoException("Somente casos publicados podem ter o alerta retirado");
        }
        Status = StatusCaso.VALIDADO;
    }
}
