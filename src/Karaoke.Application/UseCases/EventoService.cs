using Karaoke.Application.Common;
using Karaoke.Application.DTOs;
using Karaoke.Application.Interfaces;
using Karaoke.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Karaoke.Application.UseCases;

/// <summary>Ciclo de vida do evento: criar, entrar com código, mudar modo e encerrar.</summary>
public sealed class EventoService(IKaraokeDbContext db, FilaService fila)
{
    public async Task<Result<EventoDto>> CriarAsync(string hostUserId, CriarEventoRequest req, CancellationToken ct = default)
    {
        var nome = req.Nome?.Trim() ?? string.Empty;
        if (nome.Length is < 3 or > 100)
            return Result<EventoDto>.Falha(TipoErro.Invalido, "O nome do evento deve ter entre 3 e 100 caracteres.");

        string? codigo = null;
        for (var tentativa = 0; tentativa < 8; tentativa++)
        {
            var candidato = Evento.GerarCodigo();
            if (!await db.Eventos.AnyAsync(e => e.Codigo == candidato, ct))
            {
                codigo = candidato;
                break;
            }
        }

        if (codigo is null)
            return Result<EventoDto>.Falha(TipoErro.Conflito, "Não foi possível gerar um código único. Tente novamente.");

        var evento = new Evento
        {
            Codigo = codigo,
            Nome = nome,
            HostUserId = hostUserId,
            Modo = req.Modo
        };

        db.Eventos.Add(evento);
        await db.SaveChangesAsync(ct);
        return Result<EventoDto>.Ok(ParaDto(evento));
    }

    public async Task<IReadOnlyList<EventoDto>> ListarDoHostAsync(string hostUserId, CancellationToken ct = default) =>
        await db.Eventos
            .AsNoTracking()
            .Where(e => e.HostUserId == hostUserId)
            .OrderByDescending(e => e.DataCriacao)
            .Select(e => new EventoDto(e.Codigo, e.Nome, e.Modo, e.Ativo, e.DataCriacao))
            .ToListAsync(ct);

    public async Task<Result<EventoDto>> ObterAsync(string codigo, CancellationToken ct = default)
    {
        codigo = Evento.NormalizarCodigo(codigo);
        var evento = await db.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Codigo == codigo, ct);

        return evento is null
            ? Result<EventoDto>.Falha(TipoErro.NaoEncontrado, "Evento não encontrado.")
            : Result<EventoDto>.Ok(ParaDto(evento));
    }

    public async Task<Result<EntrouNoEventoDto>> EntrarAsync(string codigo, EntrarEventoRequest req, CancellationToken ct = default)
    {
        codigo = Evento.NormalizarCodigo(codigo);
        var nome = req.Nome?.Trim() ?? string.Empty;

        if (nome.Length is < 1 or > 40)
            return Result<EntrouNoEventoDto>.Falha(TipoErro.Invalido, "Informe um nome de até 40 caracteres.");

        var evento = await db.Eventos.FirstOrDefaultAsync(e => e.Codigo == codigo, ct);
        if (evento is null)
            return Result<EntrouNoEventoDto>.Falha(TipoErro.NaoEncontrado, "Evento não encontrado.");
        if (!evento.Ativo)
            return Result<EntrouNoEventoDto>.Falha(TipoErro.Conflito, "Este evento já foi encerrado.");

        var participante = new Participante { EventoId = evento.Id, Nome = nome };
        db.Participantes.Add(participante);
        await db.SaveChangesAsync(ct);

        await fila.NotificarAsync(evento, ct);

        return Result<EntrouNoEventoDto>.Ok(
            new EntrouNoEventoDto(participante.Id, participante.Token, participante.Nome, ParaDto(evento)));
    }

    public async Task<Result> AlterarModoAsync(string codigo, string hostUserId, AlterarModoRequest req, CancellationToken ct = default)
    {
        var (evento, erro) = await ObterComoHostAsync(codigo, hostUserId, ct);
        if (erro is not null) return Result.Falha(erro);

        evento!.Modo = req.Modo;
        await db.SaveChangesAsync(ct);
        await fila.NotificarAsync(evento, ct);
        return Result.Ok();
    }

    public async Task<Result> EncerrarAsync(string codigo, string hostUserId, CancellationToken ct = default)
    {
        var (evento, erro) = await ObterComoHostAsync(codigo, hostUserId, ct);
        if (erro is not null) return Result.Falha(erro);

        evento!.Encerrar();
        await db.SaveChangesAsync(ct);
        await fila.NotificarAsync(evento, ct);
        return Result.Ok();
    }

    private async Task<(Evento? Evento, Erro? Erro)> ObterComoHostAsync(string codigo, string hostUserId, CancellationToken ct)
    {
        codigo = Evento.NormalizarCodigo(codigo);
        var evento = await db.Eventos.FirstOrDefaultAsync(e => e.Codigo == codigo, ct);

        if (evento is null) return (null, new Erro(TipoErro.NaoEncontrado, "Evento não encontrado."));
        if (evento.HostUserId != hostUserId) return (null, new Erro(TipoErro.NaoAutorizado, "Apenas o anfitrião pode fazer isso."));
        return (evento, null);
    }

    private static EventoDto ParaDto(Evento e) => new(e.Codigo, e.Nome, e.Modo, e.Ativo, e.DataCriacao);
}
