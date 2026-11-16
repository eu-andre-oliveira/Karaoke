namespace Karaoke.Domain.Enums;

public enum ModoFila
{
    /// <summary>Primeiro a entrar, primeiro a cantar.</summary>
    Fifo = 0,

    /// <summary>Cada pessoa canta uma vez antes de qualquer um repetir.</summary>
    RoundRobin = 1
}
