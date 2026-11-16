using Karaoke.Application.DTOs;
using Karaoke.Application.Interfaces;
using Karaoke.Application.UseCases;
using Karaoke.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Karaoke.Api.Hubs;

/// <summary>
/// Canal em tempo real. Cada evento é um grupo; o servidor empurra a fila inteira a cada mudança.
/// As ações (adicionar, iniciar, pular...) continuam sendo chamadas REST, que passam pelas regras de autorização.
/// </summary>
public sealed class FilaHub(FilaService fila) : Hub
{
    public const string EventoCliente = "FilaAtualizada";

    /// <summary>Entra no grupo do evento e já devolve a foto atual da fila.</summary>
    public async Task<FilaDto?> EntrarNoEvento(string codigo)
    {
        var resultado = await fila.ObterAsync(codigo, Context.ConnectionAborted);
        if (!resultado.Sucesso) return null;

        await Groups.AddToGroupAsync(Context.ConnectionId, Evento.NormalizarCodigo(codigo));
        return resultado.Valor;
    }

    public Task SairDoEvento(string codigo) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, Evento.NormalizarCodigo(codigo));
}

public sealed class SignalRFilaNotifier(IHubContext<FilaHub> hub) : IFilaNotifier
{
    public Task FilaAtualizadaAsync(string codigoEvento, FilaDto fila, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(Evento.NormalizarCodigo(codigoEvento))
            .SendAsync(FilaHub.EventoCliente, fila, cancellationToken);
}
