using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Karaoke.Domain.Entities;

namespace Karaoke.Infrastructure.Data;

public class KaraokeDbContext : IdentityDbContext<ApplicationUser>
{
    public KaraokeDbContext(DbContextOptions<KaraokeDbContext> options) : base(options) { }

    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<Musica> Musicas => Set<Musica>();
    public DbSet<MusicaBiblioteca> MusicasBiblioteca => Set<MusicaBiblioteca>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Playlist>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Nome).IsRequired().HasMaxLength(200);
            b.Property(x => x.Descricao).HasMaxLength(500);
            b.Property(x => x.UsuarioId).IsRequired();
            
            b.HasOne(x => x.Usuario)
                .WithMany(x => x.Playlists)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            
            b.HasMany(x => x.Musicas)
                .WithOne(x => x.Playlist)
                .HasForeignKey(x => x.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Musica>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Titulo).IsRequired().HasMaxLength(200);
            b.Property(x => x.Artista).HasMaxLength(200);
            b.Property(x => x.Cantor).HasMaxLength(200);
            b.Property(x => x.UrlStreaming).IsRequired().HasMaxLength(500);
            b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
            
            b.HasOne(x => x.MusicaBiblioteca)
                .WithMany()
                .HasForeignKey(x => x.MusicaBibliotecaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MusicaBiblioteca>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Titulo).IsRequired().HasMaxLength(200);
            b.Property(x => x.Artista).HasMaxLength(200);
            b.Property(x => x.UrlStreaming).IsRequired().HasMaxLength(500);
            b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
            b.Property(x => x.MotivoBloqueio).HasMaxLength(500);
            
            b.HasOne(x => x.CriadoPor)
                .WithMany()
                .HasForeignKey(x => x.CriadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.Titulo);
            b.HasIndex(x => x.Artista);
        });
    }
}