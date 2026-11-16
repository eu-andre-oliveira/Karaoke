using System.Security.Cryptography;
using Karaoke.Domain.Enums;

namespace Karaoke.Domain.Entities;

/// <summary>Uma sessão de karaokê (festa, bar) identificada por um código curto.</summary>
public class Evento
{
    /// <summary>Quantas músicas cada participante pode ter aguardando ao mesmo tempo.</summary>
    public const int MaxItensAguardandoPorParticipante = 3;

    // Sem 0/O/1/I para evitar confusão ao digitar o código no celular.
    private const string AlfabetoCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public const int TamanhoCodigo = 6;

    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string HostUserId { get; set; } = null!;
    public ModoFila Modo { get; set; } = ModoFila.Fifo;
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataEncerramento { get; set; }

    public ApplicationUser Host { get; set; } = null!;
    public ICollection<Participante> Participantes { get; set; } = new List<Participante>();
    public ICollection<ItemFila> Itens { get; set; } = new List<ItemFila>();

    public void Encerrar()
    {
        Ativo = false;
        DataEncerramento = DateTime.UtcNow;
    }

    public static string GerarCodigo()
    {
        return string.Create(TamanhoCodigo, 0, static (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = AlfabetoCodigo[RandomNumberGenerator.GetInt32(AlfabetoCodigo.Length)];
        });
    }

    public static string NormalizarCodigo(string? codigo) =>
        (codigo ?? string.Empty).Trim().ToUpperInvariant();
}
