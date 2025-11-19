namespace Karaoke.Web.Models;

public class MusicaBibliotecaSearchViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Artista { get; set; }
    public string UrlStreaming { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public int QuantidadeUsos { get; set; }
    public bool Bloqueada { get; set; }
}