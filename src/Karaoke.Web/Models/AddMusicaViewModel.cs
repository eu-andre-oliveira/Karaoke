using System.ComponentModel.DataAnnotations;

namespace Karaoke.Web.Models;

public class AddMusicaViewModel
{
    public int PlaylistId { get; set; }
    public string PlaylistNome { get; set; } = null!;

    [Required(ErrorMessage = "O título da música é obrigatório")]
    [StringLength(200)]
    [Display(Name = "Título da Música")]
    public string Titulo { get; set; } = null!;

    [StringLength(200)]
    [Display(Name = "Artista")]
    public string? Artista { get; set; }

    [StringLength(200)]
    [Display(Name = "Cantor")]
    public string? Cantor { get; set; } // Novo campo

    [Required(ErrorMessage = "A URL do vídeo é obrigatória")]
    [StringLength(500)]
    [Url]
    [Display(Name = "URL do Vídeo (YouTube, Spotify, etc)")]
    public string UrlStreaming { get; set; } = null!;

    [StringLength(500)]
    [Url]
    [Display(Name = "URL da Thumbnail (opcional)")]
    public string? ThumbnailUrl { get; set; }
}