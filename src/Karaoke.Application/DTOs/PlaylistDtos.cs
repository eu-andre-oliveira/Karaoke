namespace Karaoke.Application.DTOs;

public sealed record PlaylistResumoDto(
    int Id,
    string Nome,
    string? Descricao,
    DateTime DataCriacao,
    int TotalMusicas);

public sealed record MusicaDto(
    int Id,
    string Titulo,
    string? Artista,
    string? Cantor,
    string UrlStreaming,
    string? ThumbnailUrl,
    bool JaCantada,
    int Ordem);

public sealed record PlaylistDetalheDto(
    int Id,
    string Nome,
    string? Descricao,
    DateTime DataCriacao,
    IReadOnlyList<MusicaDto> Musicas);

public sealed record SalvarPlaylistRequest(string Nome, string? Descricao);

public sealed record AdicionarMusicaRequest(
    string Titulo,
    string? Artista,
    string? Cantor,
    string UrlStreaming,
    string? ThumbnailUrl);

public sealed record AdicionarDaBibliotecaRequest(int BibliotecaId, string? Cantor);

public sealed record ReordenarMusicasRequest(IReadOnlyList<int> Ids);

public sealed record MarcarCantadaRequest(bool Cantada);

public sealed record MusicaBibliotecaDto(
    int Id,
    string Titulo,
    string? Artista,
    string UrlStreaming,
    string? ThumbnailUrl,
    int QuantidadeUsos);
