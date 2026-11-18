using FindMe.Data;
using FindMe.Dtos;
using FindMe.Exceptions;
using FindMe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FindMe.Controllers;

[ApiController]
[Route("api/alertas")]
public class AlertasController : ControllerBase
{
    private readonly FindMeDbContext _db;

    public AlertasController(FindMeDbContext db) => _db = db;

    /// <summary>Lista alertas. Sem o parâmetro "ativo" devolve todos; ativo=true é a visão pública.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertaResponse>>> Listar(
        [FromQuery] bool? ativo,
        [FromQuery] string? nome,
        [FromQuery] int? idadeMin,
        [FromQuery] int? idadeMax,
        CancellationToken ct)
    {
        if (idadeMin.HasValue && idadeMax.HasValue && idadeMin > idadeMax)
        {
            throw new RequisicaoInvalidaException("idadeMin não pode ser maior que idadeMax");
        }

        var query = _db.Alertas.AsNoTracking().Include(a => a.Caso).AsQueryable();

        if (ativo.HasValue)
        {
            var a = ativo.Value;
            query = query.Where(x => x.Ativo == a);
        }
        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.Trim().ToLower();
            query = query.Where(x => x.Caso.NomeDesaparecido.ToLower().Contains(termo));
        }
        if (idadeMin.HasValue)
        {
            var min = idadeMin.Value;
            query = query.Where(x => x.Caso.Idade >= min);
        }
        if (idadeMax.HasValue)
        {
            var max = idadeMax.Value;
            query = query.Where(x => x.Caso.Idade <= max);
        }

        var alertas = await query.OrderByDescending(x => x.DataPublicacao).ToListAsync(ct);
        return Ok(alertas.Select(AlertaResponse.From));
    }

    /// <summary>Alertas ativos publicados nos últimos N dias (feed "alertas recentes").</summary>
    [HttpGet("recentes")]
    public async Task<ActionResult<IEnumerable<AlertaResponse>>> Recentes([FromQuery] int dias = 7, CancellationToken ct = default)
    {
        if (dias < 1 || dias > 365)
        {
            throw new RequisicaoInvalidaException("dias deve estar entre 1 e 365");
        }

        var desde = DateTime.UtcNow.AddDays(-dias);
        var alertas = await _db.Alertas.AsNoTracking().Include(a => a.Caso)
            .Where(a => a.Ativo && a.DataPublicacao >= desde)
            .OrderByDescending(a => a.DataPublicacao)
            .ToListAsync(ct);
        return Ok(alertas.Select(AlertaResponse.From));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AlertaResponse>> BuscarPorId(long id, CancellationToken ct)
    {
        var alerta = await _db.Alertas.AsNoTracking().Include(a => a.Caso).FirstOrDefaultAsync(a => a.Id == id, ct)
                     ?? throw new RecursoNaoEncontradoException("Alerta", id);
        return Ok(AlertaResponse.From(alerta));
    }

    /// <summary>
    /// Publica o alerta de um caso. O caso precisa estar VALIDADO (vira PUBLICADO) e não pode ter alerta ainda.
    /// Alerta e mudança de status são gravados juntos na mesma transação (um único SaveChanges).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AlertaResponse>> Criar([FromBody] AlertaCreateRequest req, CancellationToken ct)
    {
        var casoId = req.CasoId!.Value;

        var caso = await _db.Casos.FirstOrDefaultAsync(c => c.Id == casoId, ct)
                   ?? throw new RecursoNaoEncontradoException("Caso", casoId);

        if (await _db.Alertas.AnyAsync(a => a.CasoId == casoId, ct))
        {
            throw new ConflitoException($"Já existe um alerta para o caso {casoId}");
        }

        caso.AlterarStatus(StatusCaso.PUBLICADO); // 409 se o caso não estiver VALIDADO

        var alerta = new Alerta
        {
            Caso = caso,
            Titulo = req.Titulo.Trim(),
            DescricaoPublica = req.DescricaoPublica.Trim(),
            DataPublicacao = DateTime.UtcNow,
            Ativo = true
        };

        _db.Alertas.Add(alerta);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(BuscarPorId), new { id = alerta.Id }, AlertaResponse.From(alerta));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AlertaUpdateRequest req, CancellationToken ct)
    {
        var alerta = await _db.Alertas.FirstOrDefaultAsync(a => a.Id == id, ct)
                     ?? throw new RecursoNaoEncontradoException("Alerta", id);

        alerta.Titulo = req.Titulo.Trim();
        alerta.DescricaoPublica = req.DescricaoPublica.Trim();

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Retira o alerta. Se o caso estava PUBLICADO/EM_ACOMPANHAMENTO, volta para VALIDADO.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id, CancellationToken ct)
    {
        var alerta = await _db.Alertas.Include(a => a.Caso).FirstOrDefaultAsync(a => a.Id == id, ct)
                     ?? throw new RecursoNaoEncontradoException("Alerta", id);

        if (alerta.Caso.Status == StatusCaso.PUBLICADO || alerta.Caso.Status == StatusCaso.EM_ACOMPANHAMENTO)
        {
            alerta.Caso.RetirarPublicacao();
        }

        _db.Alertas.Remove(alerta);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
