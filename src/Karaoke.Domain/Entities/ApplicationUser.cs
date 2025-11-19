using Microsoft.AspNetCore.Identity;

namespace Karaoke.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? NomeCompleto { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    public string? FotoPerfilUrl { get; set; }
    
    public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
}