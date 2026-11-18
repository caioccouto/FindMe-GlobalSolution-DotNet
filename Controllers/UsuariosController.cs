using FindMe.Data;
using FindMe.Dtos;
using FindMe.Exceptions;
using FindMe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FindMe.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly FindMeDbContext _db;

    public UsuariosController(FindMeDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Listar(CancellationToken ct)
    {
        var usuarios = await _db.Usuarios.AsNoTracking().OrderBy(u => u.Id).ToListAsync(ct);
        return Ok(usuarios.Select(UsuarioResponse.From));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UsuarioResponse>> BuscarPorId(long id, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct)
                      ?? throw new RecursoNaoEncontradoException("Usuário", id);
        return Ok(UsuarioResponse.From(usuario));
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Criar([FromBody] UsuarioCreateRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        await GarantirEmailLivre(email, null, ct);

        var usuario = new Usuario
        {
            Nome = req.Nome.Trim(),
            Email = email,
            Senha = BCrypt.Net.BCrypt.HashPassword(req.Senha),
            Telefone = req.Telefone,
            TipoUsuario = req.TipoUsuario!.Value
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(BuscarPorId), new { id = usuario.Id }, UsuarioResponse.From(usuario));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, [FromBody] UsuarioUpdateRequest req, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
                      ?? throw new RecursoNaoEncontradoException("Usuário", id);

        var email = req.Email.Trim().ToLowerInvariant();
        await GarantirEmailLivre(email, id, ct);

        usuario.Nome = req.Nome.Trim();
        usuario.Email = email;
        usuario.Telefone = req.Telefone;
        usuario.TipoUsuario = req.TipoUsuario!.Value;
        if (!string.IsNullOrEmpty(req.Senha))
        {
            usuario.Senha = BCrypt.Net.BCrypt.HashPassword(req.Senha);
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Excluir(long id, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
                      ?? throw new RecursoNaoEncontradoException("Usuário", id);

        if (await _db.Casos.AnyAsync(c => c.ResponsavelId == id, ct))
        {
            throw new ConflitoException("Não é possível excluir um usuário que possui casos cadastrados");
        }

        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task GarantirEmailLivre(string email, long? ignorarId, CancellationToken ct)
    {
        var existe = await _db.Usuarios.AnyAsync(u => u.Email == email && u.Id != (ignorarId ?? 0), ct);
        if (existe)
        {
            throw new ConflitoException($"E-mail já cadastrado: {email}");
        }
    }
}
