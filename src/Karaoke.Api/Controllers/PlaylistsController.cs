using Karaoke.Application.DTOs;
using Karaoke.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Karaoke.Api.Controllers;

[Route("api/playlists")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public sealed class PlaylistsController(PlaylistService playlists) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlaylistResumoDto>>> Listar(CancellationToken ct) =>
        Ok(await playlists.ListarAsync(UserId, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<PlaylistDetalheDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obter(int id, CancellationToken ct) =>
        Resposta(await playlists.ObterAsync(UserId, id, ct));

    [HttpPost]
    [ProducesResponseType<PlaylistDetalheDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(SalvarPlaylistRequest req, CancellationToken ct) =>
        Resposta(await playlists.CriarAsync(UserId, req, ct),
            p => CreatedAtAction(nameof(Obter), new { id = p.Id }, p));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, SalvarPlaylistRequest req, CancellationToken ct) =>
        Resposta(await playlists.AtualizarAsync(UserId, id, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct) =>
        Resposta(await playlists.ExcluirAsync(UserId, id, ct));

    [HttpPost("{id:int}/musicas")]
    [ProducesResponseType<MusicaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdicionarMusica(int id, AdicionarMusicaRequest req, CancellationToken ct) =>
        Resposta(await playlists.AdicionarMusicaAsync(UserId, id, req, ct), m => StatusCode(StatusCodes.Status201Created, m));

    [HttpPost("{id:int}/musicas/biblioteca")]
    [ProducesResponseType<MusicaDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> AdicionarDaBiblioteca(int id, AdicionarDaBibliotecaRequest req, CancellationToken ct) =>
        Resposta(await playlists.AdicionarDaBibliotecaAsync(UserId, id, req, ct), m => StatusCode(StatusCodes.Status201Created, m));

    [HttpDelete("{id:int}/musicas/{musicaId:int}")]
    public async Task<IActionResult> RemoverMusica(int id, int musicaId, CancellationToken ct) =>
        Resposta(await playlists.RemoverMusicaAsync(UserId, id, musicaId, ct));

    [HttpPut("{id:int}/musicas/ordem")]
    public async Task<IActionResult> Reordenar(int id, ReordenarMusicasRequest req, CancellationToken ct) =>
        Resposta(await playlists.ReordenarAsync(UserId, id, req, ct));

    [HttpPut("{id:int}/musicas/{musicaId:int}/cantada")]
    public async Task<IActionResult> MarcarCantada(int id, int musicaId, MarcarCantadaRequest req, CancellationToken ct) =>
        Resposta(await playlists.MarcarCantadaAsync(UserId, id, musicaId, req, ct));
}
