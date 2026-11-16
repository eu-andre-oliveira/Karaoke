using Karaoke.Domain.Enums;

namespace Karaoke.Application.DTOs;

public sealed record EventoDto(
    string Codigo,
    string Nome,
    ModoFila Modo,
    bool Ativo,
    DateTime DataCriacao);

public sealed record CriarEventoRequest(string Nome, ModoFila Modo = ModoFila.Fifo);

public sealed record AlterarModoRequest(ModoFila Modo);

public sealed record EntrarEventoRequest(string Nome);

/// <summary>Resposta ao entrar: o token deve ser guardado pelo cliente e enviado nas próximas chamadas.</summary>
public sealed record EntrouNoEventoDto(
    Guid ParticipanteId,
    string Token,
    string Nome,
    EventoDto Evento);

/// <summary>Música pode vir da biblioteca (BibliotecaId) ou de um link avulso do YouTube (Url + Titulo).</summary>
public sealed record AdicionarItemRequest(
    int? BibliotecaId,
    string? Titulo,
    string? Artista,
    string? Url);

public sealed record ItemFilaDto(
    int Id,
    string Titulo,
    string? Artista,
    string VideoId,
    string? ThumbnailUrl,
    Guid ParticipanteId,
    string Cantor,
    StatusItemFila Status,
    DateTime AdicionadoEm);

/// <summary>Foto completa da fila. É o que o SignalR envia a cada mudança.</summary>
public sealed record FilaDto(
    string Codigo,
    string Nome,
    ModoFila Modo,
    bool Ativo,
    ItemFilaDto? Atual,
    IReadOnlyList<ItemFilaDto> Aguardando,
    int TotalCantadas,
    int TotalParticipantes);
