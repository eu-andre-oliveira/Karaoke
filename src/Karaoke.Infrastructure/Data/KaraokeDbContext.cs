using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Karaoke.Application.Interfaces;
using Karaoke.Domain.Entities;

namespace Karaoke.Infrastructure.Data;

public class KaraokeDbContext : IdentityDbContext<ApplicationUser>, IKaraokeDbContext
{
    public KaraokeDbContext(DbContextOptions<KaraokeDbContext> options) : base(options) { }

    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<Musica> Musicas => Set<Musica>();
    public DbSet<MusicaBiblioteca> MusicasBiblioteca => Set<MusicaBiblioteca>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Participante> Participantes => Set<Participante>();
    public DbSet<ItemFila> ItensFila => Set<ItemFila>();

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

        modelBuilder.Entity<Evento>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Codigo).IsRequired().HasMaxLength(Evento.TamanhoCodigo);
            b.Property(x => x.Nome).IsRequired().HasMaxLength(100);
            b.Property(x => x.HostUserId).IsRequired();
            b.Property(x => x.Modo).HasConversion<string>().HasMaxLength(20);

            b.HasIndex(x => x.Codigo).IsUnique();
            b.HasIndex(x => x.HostUserId);

            b.HasOne(x => x.Host)
                .WithMany()
                .HasForeignKey(x => x.HostUserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Participantes)
                .WithOne(x => x.Evento)
                .HasForeignKey(x => x.EventoId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Itens)
                .WithOne(x => x.Evento)
                .HasForeignKey(x => x.EventoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Participante>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Nome).IsRequired().HasMaxLength(40);
            b.Property(x => x.Token).IsRequired().HasMaxLength(64);
            b.HasIndex(x => x.Token).IsUnique();

            // Restrict evita "multiple cascade paths" no SQL Server (Evento -> Itens e Evento -> Participante -> Itens).
            b.HasMany(x => x.Itens)
                .WithOne(x => x.Participante)
                .HasForeignKey(x => x.ParticipanteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ItemFila>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Titulo).IsRequired().HasMaxLength(200);
            b.Property(x => x.Artista).HasMaxLength(200);
            b.Property(x => x.UrlStreaming).IsRequired().HasMaxLength(500);
            b.Property(x => x.VideoId).IsRequired().HasMaxLength(11);
            b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            b.HasIndex(x => new { x.EventoId, x.Status });
        });
    }
}