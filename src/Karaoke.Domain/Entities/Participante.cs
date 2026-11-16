using System.Security.Cryptography;

namespace Karaoke.Domain.Entities;

/// <summary>Convidado de um evento. Entra só com nome + código, sem cadastro.</summary>
public class Participante
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int EventoId { get; set; }
    public string Nome { get; set; } = null!;

    /// <summary>Token aleatório usado pelo convidado para provar quem ele é.</summary>
    public string Token { get; set; } = GerarToken();

    public DateTime DataEntrada { get; set; } = DateTime.UtcNow;

    public Evento Evento { get; set; } = null!;
    public ICollection<ItemFila> Itens { get; set; } = new List<ItemFila>();

    public static string GerarToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
}
