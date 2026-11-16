using Karaoke.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Karaoke.Application.Interfaces;

/// <summary>Contrato de persistência que a camada Application enxerga.</summary>
public interface IKaraokeDbContext
{
    DbSet<Playlist> Playlists { get; }
    DbSet<Musica> Musicas { get; }
    DbSet<MusicaBiblioteca> MusicasBiblioteca { get; }
    DbSet<Evento> Eventos { get; }
    DbSet<Participante> Participantes { get; }
    DbSet<ItemFila> ItensFila { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
