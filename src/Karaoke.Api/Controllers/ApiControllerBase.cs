using System.Security.Claims;
using Karaoke.Application.Common;
using Karaoke.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Karaoke.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    public const string HeaderTokenParticipante = "X-Participante-Token";

    protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? throw new InvalidOperationException("Usuário não autenticado.");

    protected string? TokenParticipante =>
        Request.Headers.TryGetValue(HeaderTokenParticipante, out var v) ? v.ToString() : null;

    /// <summary>Quem está agindo: o anfitrião logado ou um convidado com token.</summary>
    protected Ator AtorAtual =>
        User.Identity?.IsAuthenticated == true ? Ator.Host(UserId) : Ator.Convidado(TokenParticipante);

    protected IActionResult Resposta(Result resultado, Func<IActionResult>? sucesso = null) =>
        resultado.Sucesso ? (sucesso?.Invoke() ?? NoContent()) : Falha(resultado.Erro!);

    protected IActionResult Resposta<T>(Result<T> resultado, Func<T, IActionResult>? sucesso = null) =>
        resultado.Sucesso ? (sucesso?.Invoke(resultado.Valor!) ?? Ok(resultado.Valor)) : Falha(resultado.Erro!);

    private ObjectResult Falha(Erro erro)
    {
        var status = erro.Tipo switch
        {
            TipoErro.NaoEncontrado => StatusCodes.Status404NotFound,
            TipoErro.Invalido => StatusCodes.Status400BadRequest,
            TipoErro.NaoAutorizado => StatusCodes.Status403Forbidden,
            TipoErro.Conflito => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(detail: erro.Mensagem, statusCode: status);
    }
}
