using System.ComponentModel.DataAnnotations;

namespace Karaoke.Web.Models;

public class EditPlaylistViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome da playlist é obrigatório")]
    [StringLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres")]
    [Display(Name = "Nome da Playlist")]
    public string Nome { get; set; } = null!;

    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
    [Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    public List<MusicaViewModel> Musicas { get; set; } = new();
}