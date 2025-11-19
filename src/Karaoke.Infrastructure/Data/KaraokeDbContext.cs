using Microsoft.EntityFrameworkCore;
using Karaoke.Domain.Entities;

namespace Karaoke.Infrastructure.Data;

public class KaraokeDbContext : DbContext
{
    public KaraokeDbContext(DbContextOptions<KaraokeDbContext> options) : base(options) { }

    public DbSet<Musica> Musicas => Set<Musica>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Musica>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Titulo).IsRequired().HasMaxLength(200);
            b.Property(x => x.Artista).HasMaxLength(200);
            b.Property(x => x.Ano);
        });
    }
}