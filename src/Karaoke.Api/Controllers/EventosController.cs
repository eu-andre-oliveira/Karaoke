using Karaoke.Application.DTOs;
using Karaoke.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karaoke.Api.Controllers;

[Route("api/eventos")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public sealed class EventosController(EventoService eventos, FilaService fila) : ApiControllerBase
{
    // ---------- Anfitrião ----------

    [HttpPost]
    [Authorize]
    [ProducesResponseType<EventoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(CriarEventoRequest req, CancellationToken ct) =>
        Resposta(await eventos.CriarAsync(UserId, req, ct),
            e => CreatedAtAction(nameof(Obter), new { codigo = e.Codigo }, e));

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<EventoDto>>> MeusEventos(CancellationToken ct) =>
        Ok(await eventos.ListarDoHostAsync(UserId, ct));

    [HttpPut("{codigo}/modo")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AlterarModo(string codigo, AlterarModoRequest req, CancellationToken ct) =>
        Resposta(await eventos.AlterarModoAsync(codigo, UserId, req, ct));

    [HttpPost("{codigo}/encerrar")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Encerrar(string codigo, CancellationToken ct) =>
        Resposta(await eventos.EncerrarAsync(codigo, UserId, ct));

    [HttpPost("{codigo}/fila/proxima")]
    [Authorize]
    [ProducesResponseType<ItemFilaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IniciarProxima(string codigo, CancellationToken ct) =>
        Resposta(await fila.IniciarProximaAsync(codigo, UserId, ct));

    [HttpPost("{codigo}/fila/atual/finalizar")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Finalizar(string codigo, CancellationToken ct) =>
        Resposta(await fila.FinalizarAtualAsync(codigo, UserId, ct));

    [HttpPost("{codigo}/fila/atual/pular")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pular(string codigo, CancellationToken ct) =>
        Resposta(await fila.PularAtualAsync(codigo, UserId, ct));

    // ---------- Público / convidado ----------

    [HttpGet("{codigo}")]
    [AllowAnonymous]
    [ProducesResponseType<EventoDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obter(string codigo, CancellationToken ct) =>
        Resposta(await eventos.ObterAsync(codigo, ct));

    [HttpPost("{codigo}/participantes")]
    [AllowAnonymous]
    [EnableRateLimiting("publico")]
    [ProducesResponseType<EntrouNoEventoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Entrar(string codigo, EntrarEventoRequest req, CancellationToken ct) =>
        Resposta(await eventos.EntrarAsync(codigo, req, ct));

    [HttpGet("{codigo}/fila")]
    [AllowAnonymous]
    [ProducesResponseType<FilaDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterFila(string codigo, CancellationToken ct) =>
        Resposta(await fila.ObterAsync(codigo, ct));

    /// <summary>Convidado escolhe uma música. Autentica pelo cabeçalho X-Participante-Token.</summary>
    [HttpPost("{codigo}/fila")]
    [AllowAnonymous]
    [ProducesResponseType<ItemFilaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Adicionar(string codigo, AdicionarItemRequest req, CancellationToken ct) =>
        Resposta(await fila.AdicionarAsync(codigo, TokenParticipante, req, ct),
            i => StatusCode(StatusCodes.Status201Created, i));

    /// <summary>O anfitrião remove qualquer item; o convidado só os seus (e só enquanto aguardam).</summary>
    [HttpDelete("{codigo}/fila/{itemId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(string codigo, int itemId, CancellationToken ct) =>
        Resposta(await fila.RemoverAsync(codigo, itemId, AtorAtual, ct));
}
