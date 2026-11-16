using Karaoke.Application.Common;
using Karaoke.Application.DTOs;
using Karaoke.Application.Interfaces;
using Karaoke.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Karaoke.Application.UseCases;

/// <summary>Playlists (repertórios) de um usuário cadastrado. Antes essa lógica vivia no controller MVC.</summary>
public sealed class PlaylistService(IKaraokeDbContext db)
{
    public async Task<IReadOnlyList<PlaylistResumoDto>> ListarAsync(string userId, CancellationToken ct = default) =>
        await db.Playlists
            .Where(p => p.UsuarioId == userId)
            .OrderByDescending(p => p.DataCriacao)
            .Select(p => new PlaylistResumoDto(p.Id, p.Nome, p.Descricao, p.DataCriacao, p.Musicas.Count))
            .ToListAsync(ct);

    public async Task<Result<PlaylistDetalheDto>> ObterAsync(string userId, int id, CancellationToken ct = default)
    {
        var playlist = await db.Playlists
            .AsNoTracking()
            .Include(p => p.Musicas)
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == userId, ct);

        return playlist is null
            ? Result<PlaylistDetalheDto>.Falha(TipoErro.NaoEncontrado, "Playlist não encontrada.")
            : Result<PlaylistDetalheDto>.Ok(ParaDetalhe(playlist));
    }

    public async Task<Result<PlaylistDetalheDto>> CriarAsync(string userId, SalvarPlaylistRequest req, CancellationToken ct = default)
    {
        var erro = Validar(req);
        if (erro is not null) return Result<PlaylistDetalheDto>.Falha(erro);

        var playlist = new Playlist
        {
            Nome = req.Nome.Trim(),
            Descricao = string.IsNullOrWhiteSpace(req.Descricao) ? null : req.Descricao.Trim(),
            UsuarioId = userId
        };

        db.Playlists.Add(playlist);
        await db.SaveChangesAsync(ct);
        return Result<PlaylistDetalheDto>.Ok(ParaDetalhe(playlist));
    }

    public async Task<Result> AtualizarAsync(string userId, int id, SalvarPlaylistRequest req, CancellationToken ct = default)
    {
        var erro = Validar(req);
        if (erro is not null) return Result.Falha(erro);

        var playlist = await db.Playlists.FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == userId, ct);
        if (playlist is null) return Result.Falha(TipoErro.NaoEncontrado, "Playlist não encontrada.");

        playlist.Nome = req.Nome.Trim();
        playlist.Descricao = string.IsNullOrWhiteSpace(req.Descricao) ? null : req.Descricao.Trim();
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> ExcluirAsync(string userId, int id, CancellationToken ct = default)
    {
        var playlist = await db.Playlists.FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == userId, ct);
        if (playlist is null) return Result.Falha(TipoErro.NaoEncontrado, "Playlist não encontrada.");

        db.Playlists.Remove(playlist);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<MusicaDto>> AdicionarMusicaAsync(string userId, int playlistId, AdicionarMusicaRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Titulo) || req.Titulo.Length > 200)
            return Result<MusicaDto>.Falha(TipoErro.Invalido, "Informe um título de até 200 caracteres.");
        if (string.IsNullOrWhiteSpace(req.UrlStreaming) || req.UrlStreaming.Length > 500)
            return Result<MusicaDto>.Falha(TipoErro.Invalido, "Informe o link da música (até 500 caracteres).");

        if (!await db.Playlists.AnyAsync(p => p.Id == playlistId && p.UsuarioId == userId, ct))
            return Result<MusicaDto>.Falha(TipoErro.NaoEncontrado, "Playlist não encontrada.");

        // Toda música nova alimenta a biblioteca compartilhada; se já existe, só conta o uso.
        var biblioteca = await db.MusicasBiblioteca
            .FirstOrDefaultAsync(m => m.UrlStreaming == req.UrlStreaming && !m.Bloqueada, ct);

        if (biblioteca is null)
        {
            biblioteca = new MusicaBiblioteca
            {
                Titulo = req.Titulo.Trim(),
                Artista = req.Artista?.Trim(),
                UrlStreaming = req.UrlStreaming.Trim(),
                ThumbnailUrl = req.ThumbnailUrl,
                CriadoPorUsuarioId = userId,
                QuantidadeUsos = 1
            };
            db.MusicasBiblioteca.Add(biblioteca);
        }
        else
        {
            biblioteca.QuantidadeUsos++;
        }

        var musica = new Musica
        {
            Titulo = biblioteca.Titulo,
            Artista = req.Artista?.Trim(),
            Cantor = req.Cantor?.Trim(),
            UrlStreaming = biblioteca.UrlStreaming,
            ThumbnailUrl = req.ThumbnailUrl,
            PlaylistId = playlistId,
            MusicaBiblioteca = biblioteca,
            Ordem = await ProximaOrdemAsync(playlistId, ct)
        };

        db.Musicas.Add(musica);
        await db.SaveChangesAsync(ct);
        return Result<MusicaDto>.Ok(ParaDto(musica));
    }

    public async Task<Result<MusicaDto>> AdicionarDaBibliotecaAsync(string userId, int playlistId, AdicionarDaBibliotecaRequest req, CancellationToken ct = default)
    {
        if (!await db.Playlists.AnyAsync(p => p.Id == playlistId && p.UsuarioId == userId, ct))
            return Result<MusicaDto>.Falha(TipoErro.NaoEncontrado, "Playlist não encontrada.");

        var biblioteca = await db.MusicasBiblioteca.FirstOrDefaultAsync(m => m.Id == req.BibliotecaId && !m.Bloqueada, ct);
        if (biblioteca is null)
            return Result<MusicaDto>.Falha(TipoErro.NaoEncontrado, "Música não encontrada na biblioteca.");

        biblioteca.QuantidadeUsos++;

        var musica = new Musica
        {
            Titulo = biblioteca.Titulo,
            Artista = biblioteca.Artista,
            Cantor = req.Cantor?.Trim(),
            UrlStreaming = biblioteca.UrlStreaming,
            ThumbnailUrl = biblioteca.ThumbnailUrl,
            PlaylistId = playlistId,
            MusicaBibliotecaId = biblioteca.Id,
            Ordem = await ProximaOrdemAsync(playlistId, ct)
        };

        db.Musicas.Add(musica);
        await db.SaveChangesAsync(ct);
        return Result<MusicaDto>.Ok(ParaDto(musica));
    }

    public async Task<Result> RemoverMusicaAsync(string userId, int playlistId, int musicaId, CancellationToken ct = default)
    {
        var musica = await db.Musicas
            .Include(m => m.Playlist)
            .FirstOrDefaultAsync(m => m.Id == musicaId && m.PlaylistId == playlistId && m.Playlist.UsuarioId == userId, ct);

        if (musica is null) return Result.Falha(TipoErro.NaoEncontrado, "Música não encontrada.");

        db.Musicas.Remove(musica);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> ReordenarAsync(string userId, int playlistId, ReordenarMusicasRequest req, CancellationToken ct = default)
    {
        var playlist = await db.Playlists
            .Include(p => p.Musicas)
            .FirstOrDefaultAsync(p => p.Id == playlistId && p.UsuarioId == userId, ct);

        if (playlist is null) return Result.Falha(TipoErro.NaoEncontrado, "Playlist não encontrada.");

        for (var i = 0; i < req.Ids.Count; i++)
        {
            var musica = playlist.Musicas.FirstOrDefault(m => m.Id == req.Ids[i]);
            if (musica is not null) musica.Ordem = i;
        }

        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> MarcarCantadaAsync(string userId, int playlistId, int musicaId, MarcarCantadaRequest req, CancellationToken ct = default)
    {
        var musica = await db.Musicas
            .Include(m => m.Playlist)
            .FirstOrDefaultAsync(m => m.Id == musicaId && m.PlaylistId == playlistId && m.Playlist.UsuarioId == userId, ct);

        if (musica is null) return Result.Falha(TipoErro.NaoEncontrado, "Música não encontrada.");

        musica.JaCantada = req.Cantada;
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private async Task<int> ProximaOrdemAsync(int playlistId, CancellationToken ct) =>
        (await db.Musicas
            .Where(m => m.PlaylistId == playlistId)
            .Select(m => (int?)m.Ordem)
            .MaxAsync(ct) ?? 0) + 1;

    private static Erro? Validar(SalvarPlaylistRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome) || req.Nome.Trim().Length > 200)
            return new Erro(TipoErro.Invalido, "O nome é obrigatório e deve ter até 200 caracteres.");
        if (req.Descricao is { Length: > 500 })
            return new Erro(TipoErro.Invalido, "A descrição deve ter até 500 caracteres.");
        return null;
    }

    private static MusicaDto ParaDto(Musica m) =>
        new(m.Id, m.Titulo, m.Artista, m.Cantor, m.UrlStreaming, m.ThumbnailUrl, m.JaCantada, m.Ordem);

    private static PlaylistDetalheDto ParaDetalhe(Playlist p) =>
        new(p.Id, p.Nome, p.Descricao, p.DataCriacao,
            p.Musicas.OrderBy(m => m.Ordem).Select(ParaDto).ToList());
}
