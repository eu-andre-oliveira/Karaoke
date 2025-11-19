namespace Karaoke.Domain.Entities;

public class MusicaBiblioteca
{
    public int Id { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Artista { get; set; }
    public string UrlStreaming { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public int QuantidadeUsos { get; set; } // Contador de vezes que foi usada
    public bool Bloqueada { get; set; } // Flag para links inapropriados
    public string? MotivoBloqueio { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public string CriadoPorUsuarioId { get; set; } = null!;
    
    public ApplicationUser CriadoPor { get; set; } = null!;
}