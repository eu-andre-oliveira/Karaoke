using System.ComponentModel.DataAnnotations;

namespace Karaoke.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "A senha é obrigatória")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = null!;

    [Display(Name = "Lembrar de mim")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}