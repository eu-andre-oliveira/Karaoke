using System.ComponentModel.DataAnnotations;
using Karaoke.Domain.Constants;
using Karaoke.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karaoke.Api.Controllers;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required, StringLength(100, MinimumLength = 2)] string NomeCompleto);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    bool RememberMe = false);

public sealed record UsuarioDto(string Id, string Email, string? Nome, IReadOnlyList<string> Roles);

[Route("api/auth")]
[EnableRateLimiting("publico")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UsuarioDto>> Register(RegisterRequest req)
    {
        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            NomeCompleto = req.NomeCompleto.Trim()
        };

        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(e.Code, e.Description);
            return ValidationProblem(ModelState);
        }

        await userManager.AddToRoleAsync(user, UserRoles.User);
        await signInManager.SignInAsync(user, isPersistent: false);
        return Ok(await ParaDtoAsync(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UsuarioDto>> Login(LoginRequest req)
    {
        var result = await signInManager.PasswordSignInAsync(req.Email, req.Password, req.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Problem(detail: "Muitas tentativas. Tente novamente em alguns minutos.", statusCode: StatusCodes.Status429TooManyRequests);
        if (!result.Succeeded)
            return Problem(detail: "E-mail ou senha inválidos.", statusCode: StatusCodes.Status401Unauthorized);

        var user = await userManager.FindByEmailAsync(req.Email);
        return Ok(await ParaDtoAsync(user!));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> Me()
    {
        var user = await userManager.FindByIdAsync(UserId);
        return user is null ? Unauthorized() : Ok(await ParaDtoAsync(user));
    }

    private async Task<UsuarioDto> ParaDtoAsync(ApplicationUser user) =>
        new(user.Id, user.Email!, user.NomeCompleto, (await userManager.GetRolesAsync(user)).ToList());
}
