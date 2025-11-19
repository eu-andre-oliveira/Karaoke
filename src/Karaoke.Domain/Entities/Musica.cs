namespace Karaoke.Domain.Entities;

public class Musica
{
    public int Id { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Artista { get; set; }
    public string? Cantor { get; set; }
    public string UrlStreaming { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public int PlaylistId { get; set; }
    public bool JaCantada { get; set; }
    public int Ordem { get; set; }
    
    // Referência à biblioteca compartilhada (opcional)
    public int? MusicaBibliotecaId { get; set; }
    public MusicaBiblioteca? MusicaBiblioteca { get; set; }
    
    public Playlist Playlist { get; set; } = null!;
}