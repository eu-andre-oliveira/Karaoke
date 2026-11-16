using Karaoke.Application.DTOs;

namespace Karaoke.Application.Interfaces;

/// <summary>Avisa os clientes conectados que a fila de um evento mudou (implementado com SignalR na Api).</summary>
public interface IFilaNotifier
{
    Task FilaAtualizadaAsync(string codigoEvento, FilaDto fila, CancellationToken cancellationToken = default);
}
