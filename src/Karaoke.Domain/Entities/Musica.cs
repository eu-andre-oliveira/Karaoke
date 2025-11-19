namespace Karaoke.Domain.Entities;

public class Musica
{
    public int Id { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Artista { get; set; }
    public int Ano { get; set; }
}