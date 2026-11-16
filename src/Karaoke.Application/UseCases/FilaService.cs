using Karaoke.Application.Common;
using Karaoke.Application.DTOs;
using Karaoke.Application.Interfaces;
using Karaoke.Domain.Entities;
using Karaoke.Domain.Enums;
using Karaoke.Domain.Services;
using Karaoke.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Karaoke.Application.UseCases;

/// <summary>Quem está pedindo a ação: o anfitrião (usuário logado) ou um convidado (token).</summary>
public readonly record struct Ator(string? HostUserId, string? ParticipanteToken)
{
    public static Ator Host(string userId) => new(userId, null);
    public static Ator Convidado(string? token) => new(null, token);
}

/// <summary>Regras da fila de um evento. Cada mudança publica a fila inteira via <see cref="IFilaNotifier"/>.</summary>
public sealed class FilaService(IKaraokeDbContext db, IFilaNotifier notifier)
{
    public async Task<Result<FilaDto>> ObterAsync(string codigo, CancellationToken ct = default)
    {
        var evento = await ObterEventoAsync(codigo, ct);
        return evento is null
            ? Result<FilaDto>.Falha(TipoErro.NaoEncontrado, "Evento não encontrado.")
            : Result<FilaDto>.Ok(await MontarFilaAsync(evento, ct));
    }

    public async Task<Result<ItemFilaDto>> AdicionarAsync(string codigo, string? token, AdicionarItemRequest req, CancellationToken ct = default)
    {
        var evento = await ObterEventoAsync(codigo, ct);
        if (evento is null) return Result<ItemFilaDto>.Falha(TipoErro.NaoEncontrado, "Evento não encontrado.");
        if (!evento.Ativo) return Result<ItemFilaDto>.Falha(TipoErro.Conflito, "Este evento já foi encerrado.");

        var participante = await ObterParticipanteAsync(evento.Id, token, ct);
        if (participante is null)
            return Result<ItemFilaDto>.Falha(TipoErro.NaoAutorizado, "Entre no evento antes de escolher uma música.");

        var aguardando = await db.ItensFila.CountAsync(
            i => i.ParticipanteId == participante.Id && i.Status == StatusItemFila.Aguardando, ct);
        if (aguardando >= Evento.MaxItensAguardandoPorParticipante)
            return Result<ItemFilaDto>.Falha(TipoErro.Conflito,
                $"Você já tem {Evento.MaxItensAguardandoPorParticipante} músicas na fila. Espere uma ser cantada.");

        var item = await MontarItemAsync(evento, participante, req, ct);
        if (!item.Sucesso) return Result<ItemFilaDto>.Falha(item.Erro!);

        db.ItensFila.Add(item.Valor!);
        await db.SaveChangesAsync(ct);
        await NotificarAsync(evento, ct);

        return Result<ItemFilaDto>.Ok(ParaDto(item.Valor!, participante.Nome));
    }

    public async Task<Result> RemoverAsync(string codigo, int itemId, Ator ator, CancellationToken ct = default)
    {
        var evento = await ObterEventoAsync(codigo, ct);
        if (evento is null) return Result.Falha(TipoErro.NaoEncontrado, "Evento não encontrado.");

        var item = await db.ItensFila.FirstOrDefaultAsync(i => i.Id == itemId && i.EventoId == evento.Id, ct);
        if (item is null) return Result.Falha(TipoErro.NaoEncontrado, "Item não encontrado.");

        var ehHost = ator.HostUserId is not null && ator.HostUserId == evento.HostUserId;
        if (!ehHost)
        {
            var participante = await ObterParticipanteAsync(evento.Id, ator.ParticipanteToken, ct);
            if (participante is null || participante.Id != item.ParticipanteId)
                return Result.Falha(TipoErro.NaoAutorizado, "Você só pode remover as suas próprias músicas.");
        }

        if (item.Status != StatusItemFila.Aguardando)
            return Result.Falha(TipoErro.Conflito, "Só dá para remover músicas que ainda estão aguardando.");

        db.ItensFila.Remove(item);
        await db.SaveChangesAsync(ct);
        await NotificarAsync(evento, ct);
        return Result.Ok();
    }

    public async Task<Result<ItemFilaDto>> IniciarProximaAsync(string codigo, string hostUserId, CancellationToken ct = default)
    {
        var (evento, erro) = await ObterComoHostAsync(codigo, hostUserId, ct);
        if (erro is not null) return Result<ItemFilaDto>.Falha(erro);
        if (!evento!.Ativo) return Result<ItemFilaDto>.Falha(TipoErro.Conflito, "Este evento já foi encerrado.");

        var itens = await db.ItensFila.Include(i => i.Participante).Where(i => i.EventoId == evento.Id).ToListAsync(ct);

        if (itens.Any(i => i.Status == StatusItemFila.Cantando))
            return Result<ItemFilaDto>.Falha(TipoErro.Conflito, "Finalize ou pule a música atual antes de iniciar a próxima.");

        var proximo = OrdenadorFila.ProximoItem(itens, evento.Modo);
        if (proximo is null)
            return Result<ItemFilaDto>.Falha(TipoErro.Conflito, "A fila está vazia.");

        proximo.Status = StatusItemFila.Cantando;
        proximo.IniciadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await NotificarAsync(evento, ct);

        return Result<ItemFilaDto>.Ok(ParaDto(proximo, proximo.Participante.Nome));
    }

    public Task<Result> FinalizarAtualAsync(string codigo, string hostUserId, CancellationToken ct = default) =>
        EncerrarAtualAsync(codigo, hostUserId, StatusItemFila.Cantada, ct);

    public Task<Result> PularAtualAsync(string codigo, string hostUserId, CancellationToken ct = default) =>
        EncerrarAtualAsync(codigo, hostUserId, StatusItemFila.Pulada, ct);

    /// <summary>Monta a foto atual da fila e avisa quem está conectado.</summary>
    public async Task NotificarAsync(Evento evento, CancellationToken ct = default) =>
        await notifier.FilaAtualizadaAsync(evento.Codigo, await MontarFilaAsync(evento, ct), ct);

    private async Task<Result> EncerrarAtualAsync(string codigo, string hostUserId, StatusItemFila novoStatus, CancellationToken ct)
    {
        var (evento, erro) = await ObterComoHostAsync(codigo, hostUserId, ct);
        if (erro is not null) return Result.Falha(erro);

        var atual = await db.ItensFila.FirstOrDefaultAsync(
            i => i.EventoId == evento!.Id && i.Status == StatusItemFila.Cantando, ct);
        if (atual is null) return Result.Falha(TipoErro.Conflito, "Não há música tocando agora.");

        atual.Status = novoStatus;
        atual.FinalizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await NotificarAsync(evento!, ct);
        return Result.Ok();
    }

    private async Task<Result<ItemFila>> MontarItemAsync(Evento evento, Participante participante, AdicionarItemRequest req, CancellationToken ct)
    {
        string titulo, url;
        string? artista, thumb = null;
        int? bibliotecaId = null;

        if (req.BibliotecaId is { } id)
        {
            var musica = await db.MusicasBiblioteca.FirstOrDefaultAsync(m => m.Id == id && !m.Bloqueada, ct);
            if (musica is null)
                return Result<ItemFila>.Falha(TipoErro.NaoEncontrado, "Música não encontrada na biblioteca.");

            titulo = musica.Titulo;
            artista = musica.Artista;
            url = musica.UrlStreaming;
            thumb = musica.ThumbnailUrl;
            bibliotecaId = musica.Id;
            musica.QuantidadeUsos++;
        }
        else
        {
            titulo = req.Titulo?.Trim() ?? string.Empty;
            artista = req.Artista?.Trim();
            url = req.Url?.Trim() ?? string.Empty;

            if (titulo.Length is < 1 or > 200)
                return Result<ItemFila>.Falha(TipoErro.Invalido, "Informe o título da música (até 200 caracteres).");
        }

        if (!YouTubeUrl.TryGetVideoId(url, out var videoId))
            return Result<ItemFila>.Falha(TipoErro.Invalido, "Esse link não é um vídeo do YouTube que o player consiga tocar.");

        return Result<ItemFila>.Ok(new ItemFila
        {
            EventoId = evento.Id,
            ParticipanteId = participante.Id,
            Titulo = titulo,
            Artista = artista,
            UrlStreaming = YouTubeUrl.Watch(videoId),
            VideoId = videoId,
            ThumbnailUrl = thumb ?? YouTubeUrl.Thumbnail(videoId),
            MusicaBibliotecaId = bibliotecaId
        });
    }

    private async Task<FilaDto> MontarFilaAsync(Evento evento, CancellationToken ct)
    {
        var itens = await db.ItensFila
            .AsNoTracking()
            .Include(i => i.Participante)
            .Where(i => i.EventoId == evento.Id)
            .ToListAsync(ct);

        var atual = itens.FirstOrDefault(i => i.Status == StatusItemFila.Cantando);
        var aguardando = OrdenadorFila.OrdenarAguardando(itens, evento.Modo)
            .Select(i => ParaDto(i, i.Participante.Nome))
            .ToList();

        var totalParticipantes = await db.Participantes.CountAsync(p => p.EventoId == evento.Id, ct);

        return new FilaDto(
            evento.Codigo,
            evento.Nome,
            evento.Modo,
            evento.Ativo,
            atual is null ? null : ParaDto(atual, atual.Participante.Nome),
            aguardando,
            itens.Count(i => i.Status == StatusItemFila.Cantada),
            totalParticipantes);
    }

    private Task<Evento?> ObterEventoAsync(string codigo, CancellationToken ct)
    {
        codigo = Evento.NormalizarCodigo(codigo);
        return db.Eventos.FirstOrDefaultAsync(e => e.Codigo == codigo, ct);
    }

    private Task<Participante?> ObterParticipanteAsync(int eventoId, string? token, CancellationToken ct) =>
        string.IsNullOrWhiteSpace(token)
            ? Task.FromResult<Participante?>(null)
            : db.Participantes.FirstOrDefaultAsync(p => p.EventoId == eventoId && p.Token == token, ct);

    private async Task<(Evento? Evento, Erro? Erro)> ObterComoHostAsync(string codigo, string hostUserId, CancellationToken ct)
    {
        var evento = await ObterEventoAsync(codigo, ct);
        if (evento is null) return (null, new Erro(TipoErro.NaoEncontrado, "Evento não encontrado."));
        if (evento.HostUserId != hostUserId) return (null, new Erro(TipoErro.NaoAutorizado, "Apenas o anfitrião pode fazer isso."));
        return (evento, null);
    }

    private static ItemFilaDto ParaDto(ItemFila i, string cantor) =>
        new(i.Id, i.Titulo, i.Artista, i.VideoId, i.ThumbnailUrl, i.ParticipanteId, cantor, i.Status, i.AdicionadoEm);
}
