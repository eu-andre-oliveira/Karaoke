namespace Karaoke.Web.Models;

public class MusicaViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Artista { get; set; }
    public string? Cantor { get; set; } // Novo campo
    public string UrlStreaming { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public bool JaCantada { get; set; } // Novo campo
    public int Ordem { get; set; }
}