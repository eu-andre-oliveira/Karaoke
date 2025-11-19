namespace Karaoke.Web.Models;

public class PlaylistDetailsViewModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public DateTime DataCriacao { get; set; }
    public int TotalMusicas { get; set; }
    public List<MusicaViewModel> Musicas { get; set; } = new();
}