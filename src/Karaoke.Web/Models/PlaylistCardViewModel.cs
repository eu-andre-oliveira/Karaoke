namespace Karaoke.Web.Models;

public class PlaylistCardViewModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public int TotalMusicas { get; set; }
    public List<string> PrimeirasMusicasTitulos { get; set; } = new();
    public DateTime DataCriacao { get; set; }
}