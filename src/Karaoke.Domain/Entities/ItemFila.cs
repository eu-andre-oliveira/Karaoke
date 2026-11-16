using Karaoke.Domain.Enums;

namespace Karaoke.Domain.Entities;

public class ItemFila
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public Guid ParticipanteId { get; set; }

    // Snapshot da música no momento em que entrou na fila.
    public string Titulo { get; set; } = null!;
    public string? Artista { get; set; }
    public string UrlStreaming { get; set; } = null!;
    public string VideoId { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public int? MusicaBibliotecaId { get; set; }

    public StatusItemFila Status { get; set; } = StatusItemFila.Aguardando;
    public DateTime AdicionadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? IniciadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }

    public Evento Evento { get; set; } = null!;
    public Participante Participante { get; set; } = null!;
}
