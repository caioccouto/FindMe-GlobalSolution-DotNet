using FindMe.Data;
using FindMe.Dtos;
using FindMe.Exceptions;
using FindMe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FindMe.Controllers;

[ApiController]
[Route("api/casos")]
public class CasosController : ControllerBase
{
    private readonly FindMeDbContext _db;
    private readonly ILogger<CasosController> _logger;

    public CasosController(FindMeDbContext db, ILogger<CasosController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Lista casos. Todos os filtros são opcionais e combináveis.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CasoResponse>>> Listar(
        [FromQuery] StatusCaso? status,
        [FromQuery] string? nome,
        [FromQuery] int? idadeMin,
        [FromQuery] int? idadeMax,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] long? responsavelId,
        CancellationToken ct)
    {
        if (idadeMin.HasValue && idadeMax.HasValue && idadeMin > idadeMax)
        {
            throw new RequisicaoInvalidaException("idadeMin não pode ser maior que idadeMax");
        }

        var query = _db.Casos.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            var s = status.Value;
            query = query.Where(c => c.Status == s);
        }
        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.Trim().ToLower();
            query = query.Where(c => c.NomeDesaparecido.ToLower().Contains(termo));
        }
        if (idadeMin.HasValue)
        {
            var min = idadeMin.Value;
            query = query.Where(c => c.Idade >= min);
        }
        if (idadeMax.HasValue)
        {
            var max = idadeMax.Value;
            query = query.Where(c => c.Idade <= max);
        }
        if (dataInicio.HasValue)
        {
            var inicio = dataInicio.Value.Date;
            query = query.Where(c => c.DataDesaparecimento >= inicio);
        }
        if (dataFim.HasValue)
        {
            var fim = dataFim.Value.Date;
            query = query.Where(c => c.DataDesaparecimento <= fim);
        }
        if (responsavelId.HasValue)
        {
            var rid = responsavelId.Value;
            query = query.Where(c => c.ResponsavelId == rid);
        }

        var casos = await query.OrderBy(c => c.Id).ToListAsync(ct);
        return Ok(casos.Select(CasoResponse.From));
    }

    /// <summary>Total de casos por status (painel do moderador). Status sem casos aparecem com 0.</summary>
    [HttpGet("estatisticas")]
    public async Task<ActionResult<IEnumerable<StatusTotalResponse>>> Estatisticas(CancellationToken ct)
    {
        var agrupado = await _db.Casos.AsNoTracking()
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        var resultado = Enum.GetValues<StatusCaso>()
            .Select(s => new StatusTotalResponse(s, agrupado.FirstOrDefault(a => a.Status == s)?.Total ?? 0));
        return Ok(resultado);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CasoResponse>> BuscarPorId(long id, CancellationToken ct)
    {
        var caso = await _db.Casos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
                   ?? throw new RecursoNaoEncontradoException("Caso", id);
        return Ok(CasoResponse.From(caso));
    }

    [HttpPost]
    public async Task<ActionResult<CasoResponse>> Criar([FromBody] CasoCreateRequest req, CancellationToken ct)
    {
        var responsavelId = req.ResponsavelId!.Value;
        var responsavel = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == responsavelId, ct)
                          ?? throw new RecursoNaoEncontradoException("Usuário", responsavelId);

        if (responsavel.TipoUsuario != TipoUsuario.RESPONSAVEL)
        {
            throw new RequisicaoInvalidaException("Somente usuários do tipo RESPONSAVEL podem registrar casos");
        }

        var caso = new Caso
        {
            NomeDesaparecido = req.NomeDesaparecido.Trim(),
            Idade = req.Idade!.Value,
            Descricao = req.Descricao.Trim(),
            UltimaLocalizacaoConhecida = req.UltimaLocalizacaoConhecida.Trim(),
            DataDesaparecimento = req.DataDesaparecimento!.Value.ToDateTime(TimeOnly.MinValue),
            ResponsavelId = responsavelId
            // Status nasce EM_ANALISE (valor padrão da entidade)
        };

        _db.Casos.Add(caso);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(BuscarPorId), new { id = caso.Id }, CasoResponse.From(caso));
    }

    /// <summary>Atualiza os dados do caso. Status e responsável não mudam por aqui.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] CasoUpdateRequest req, CancellationToken ct)
    {
        var caso = await _db.Casos.FirstOrDefaultAsync(c => c.Id == id, ct)
                   ?? throw new RecursoNaoEncontradoException("Caso", id);

        caso.NomeDesaparecido = req.NomeDesaparecido.Trim();
        caso.Idade = req.Idade!.Value;
        caso.Descricao = req.Descricao.Trim();
        caso.UltimaLocalizacaoConhecida = req.UltimaLocalizacaoConhecida.Trim();
        caso.DataDesaparecimento = req.DataDesaparecimento!.Value.ToDateTime(TimeOnly.MinValue);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Transição de status. Inválida → 409. PUBLICADO só acontece ao criar o alerta (POST /api/alertas).
    /// LOCALIZADO/ENCERRADO desativam o alerta do caso.
    /// </summary>
    [HttpPatch("{id:long}/status")]
    public async Task<ActionResult<CasoResponse>> AlterarStatus(
        long id, [FromBody] AlterarStatusRequest req, CancellationToken ct)
    {
        var novo = req.Status!.Value;
        if (novo == StatusCaso.PUBLICADO)
        {
            throw new RequisicaoInvalidaException(
                "Um caso só é publicado ao criar o alerta público (POST /api/alertas)");
        }

        var caso = await _db.Casos.Include(c => c.Alerta).FirstOrDefaultAsync(c => c.Id == id, ct)
                   ?? throw new RecursoNaoEncontradoException("Caso", id);

        var anterior = caso.Status;
        caso.AlterarStatus(novo);

        if ((novo == StatusCaso.LOCALIZADO || novo == StatusCaso.ENCERRADO) && caso.Alerta != null)
        {
            caso.Alerta.Ativo = false;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("AUDITORIA caso={CasoId} status {Anterior} -> {Novo}", id, anterior, novo);
        return Ok(CasoResponse.From(caso));
    }

    /// <summary>Exclui o caso. O alerta associado é removido junto (ON DELETE CASCADE).</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id, CancellationToken ct)
    {
        var caso = await _db.Casos.FirstOrDefaultAsync(c => c.Id == id, ct)
                   ?? throw new RecursoNaoEncontradoException("Caso", id);

        _db.Casos.Remove(caso);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
