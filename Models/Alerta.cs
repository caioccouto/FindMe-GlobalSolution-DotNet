namespace FindMe.Models;

public class Alerta
{
    public long Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string DescricaoPublica { get; set; } = string.Empty;
    public DateTime DataPublicacao { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;

    public long CasoId { get; set; }
    public Caso Caso { get; set; } = null!;
}
