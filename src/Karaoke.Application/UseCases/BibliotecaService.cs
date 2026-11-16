using Karaoke.Application.DTOs;
using Karaoke.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Karaoke.Application.UseCases;

public sealed class BibliotecaService(IKaraokeDbContext db)
{
    public async Task<IReadOnlyList<MusicaBibliotecaDto>> BuscarAsync(string? termo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(termo) || termo.Trim().Length < 2)
            return [];

        var t = termo.Trim().ToLower();

        return await db.MusicasBiblioteca
            .AsNoTracking()
            .Where(m => !m.Bloqueada &&
                        (m.Titulo.ToLower().Contains(t) ||
                         (m.Artista != null && m.Artista.ToLower().Contains(t))))
            .OrderByDescending(m => m.QuantidadeUsos)
            .ThenBy(m => m.Titulo)
            .Take(10)
            .Select(m => new MusicaBibliotecaDto(m.Id, m.Titulo, m.Artista, m.UrlStreaming, m.ThumbnailUrl, m.QuantidadeUsos))
            .ToListAsync(ct);
    }
}
