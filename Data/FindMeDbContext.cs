using System.Text.RegularExpressions;
using FindMe.Models;
using Microsoft.EntityFrameworkCore;

namespace FindMe.Data;

public class FindMeDbContext : DbContext
{
    public FindMeDbContext(DbContextOptions<FindMeDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Caso> Casos => Set<Caso>();
    public DbSet<Alerta> Alertas => Set<Alerta>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("TB_USUARIO");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).IsRequired().HasMaxLength(100);
            e.Property(x => x.Email).IsRequired().HasMaxLength(255);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Senha).IsRequired().HasMaxLength(100);
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.Property(x => x.TipoUsuario).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.DataCadastro).IsRequired();
        });

        mb.Entity<Caso>(e =>
        {
            e.ToTable("TB_CASO");
            e.HasKey(x => x.Id);
            e.Property(x => x.NomeDesaparecido).IsRequired().HasMaxLength(150);
            e.Property(x => x.Idade).IsRequired();
            e.Property(x => x.Descricao).IsRequired().HasMaxLength(2000);
            e.Property(x => x.UltimaLocalizacaoConhecida).IsRequired().HasMaxLength(300);
            e.Property(x => x.DataDesaparecimento).IsRequired().HasColumnType("DATE");
            e.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

            // 1:N - Usuario (responsável) tem vários Casos. Não apaga usuário que tenha casos.
            e.HasOne(x => x.Responsavel)
                .WithMany(u => u.Casos)
                .HasForeignKey(x => x.ResponsavelId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Alerta>(e =>
        {
            e.ToTable("TB_ALERTA");
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).IsRequired().HasMaxLength(150);
            e.Property(x => x.DescricaoPublica).IsRequired().HasMaxLength(1000);
            e.Property(x => x.DataPublicacao).IsRequired();
            e.Property(x => x.Ativo).IsRequired();

            // 1:1 - um Caso tem no máximo um Alerta (índice único em CASO_ID). Apagar o caso apaga o alerta.
            e.HasOne(x => x.Caso)
                .WithOne(c => c.Alerta)
                .HasForeignKey<Alerta>(x => x.CasoId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Oracle diferencia maiúsculas/minúsculas em identificadores entre aspas. Para o SQL ficar
        // amigável no SQL Developer (e no vídeo), as colunas viram UPPER_SNAKE_CASE (NOME_DESAPARECIDO).
        foreach (var entity in mb.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ParaUpperSnake(property.Name));
            }
        }
    }

    private static string ParaUpperSnake(string nome) =>
        Regex.Replace(nome, "(?<!^)([A-Z])", "_$1").ToUpperInvariant();
}
