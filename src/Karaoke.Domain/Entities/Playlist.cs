namespace Karaoke.Domain.Entities;

public class Playlist
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public string UsuarioId { get; set; } = null!;
    
    public ApplicationUser Usuario { get; set; } = null!;
    public ICollection<Musica> Musicas { get; set; } = new List<Musica>();
}