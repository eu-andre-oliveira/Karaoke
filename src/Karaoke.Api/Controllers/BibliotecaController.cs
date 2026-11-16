using Karaoke.Application.DTOs;
using Karaoke.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Karaoke.Api.Controllers;

[Route("api/biblioteca")]
[AllowAnonymous]
public sealed class BibliotecaController(BibliotecaService biblioteca) : ApiControllerBase
{
    /// <summary>Busca pública: convidados precisam procurar músicas sem ter conta.</summary>
    [HttpGet("busca")]
    public async Task<ActionResult<IReadOnlyList<MusicaBibliotecaDto>>> Buscar([FromQuery] string? termo, CancellationToken ct) =>
        Ok(await biblioteca.BuscarAsync(termo, ct));
}
