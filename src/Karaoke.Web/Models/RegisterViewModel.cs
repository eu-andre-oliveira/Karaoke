using System.ComponentModel.DataAnnotations;

namespace Karaoke.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "O nome completo é obrigatório")]
    [Display(Name = "Nome Completo")]
    [StringLength(100)]
    public string NomeCompleto { get; set; } = null!;

    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "A senha é obrigatória")]
    [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} caracteres.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Senha")]
    [Compare("Password", ErrorMessage = "As senhas não coincidem.")]
    public string ConfirmPassword { get; set; } = null!;
}