using Karaoke.Domain.Entities;
using Karaoke.Domain.Enums;
using Karaoke.Domain.Services;

namespace Karaoke.Domain.Tests;

public class OrdenadorFilaTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 20, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Ana = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid Beto = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid Caio = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    private static int _id;

    private static ItemFila Item(Guid quem, string titulo, int minutos, StatusItemFila status = StatusItemFila.Aguardando) => new()
    {
        Id = ++_id,
        ParticipanteId = quem,
        Titulo = titulo,
        Status = status,
        AdicionadoEm = T0.AddMinutes(minutos)
    };

    private static string[] Titulos(IEnumerable<ItemFila> itens) => itens.Select(i => i.Titulo).ToArray();

    [Fact]
    public void Fifo_respeita_ordem_de_chegada()
    {
        var itens = new[] { Item(Ana, "A1", 0), Item(Ana, "A2", 1), Item(Beto, "B1", 2) };

        var ordem = OrdenadorFila.OrdenarAguardando(itens, ModoFila.Fifo);

        Assert.Equal(new[] { "A1", "A2", "B1" }, Titulos(ordem));
    }

    [Fact]
    public void RoundRobin_intercala_participantes()
    {
        var itens = new[] { Item(Ana, "A1", 0), Item(Ana, "A2", 1), Item(Ana, "A3", 2), Item(Beto, "B1", 3), Item(Caio, "C1", 4) };

        var ordem = OrdenadorFila.OrdenarAguardando(itens, ModoFila.RoundRobin);

        Assert.Equal(new[] { "A1", "B1", "C1", "A2", "A3" }, Titulos(ordem));
    }

    [Fact]
    public void RoundRobin_considera_quem_ja_cantou()
    {
        var itens = new[]
        {
            Item(Ana, "A-cantada", -10, StatusItemFila.Cantada),
            Item(Ana, "A2", 0),
            Item(Beto, "B1", 1)
        };

        var proximo = OrdenadorFila.ProximoItem(itens, ModoFila.RoundRobin);

        Assert.Equal("B1", proximo!.Titulo); // Beto ainda não cantou, Ana já
    }

    [Fact]
    public void RoundRobin_conta_a_musica_que_esta_cantando()
    {
        var itens = new[]
        {
            Item(Ana, "A-agora", -1, StatusItemFila.Cantando),
            Item(Ana, "A2", 0),
            Item(Beto, "B1", 1)
        };

        Assert.Equal("B1", OrdenadorFila.ProximoItem(itens, ModoFila.RoundRobin)!.Titulo);
    }

    [Fact]
    public void RoundRobin_empate_resolve_por_ordem_de_chegada()
    {
        var itens = new[] { Item(Beto, "B1", 0), Item(Ana, "A1", 1) };

        Assert.Equal("B1", OrdenadorFila.ProximoItem(itens, ModoFila.RoundRobin)!.Titulo);
    }

    [Fact]
    public void Ignora_itens_que_nao_estao_aguardando()
    {
        var itens = new[]
        {
            Item(Ana, "feita", 0, StatusItemFila.Cantada),
            Item(Ana, "pulada", 1, StatusItemFila.Pulada),
            Item(Beto, "B1", 2)
        };

        Assert.Equal(new[] { "B1" }, Titulos(OrdenadorFila.OrdenarAguardando(itens, ModoFila.Fifo)));
    }

    [Theory]
    [InlineData(ModoFila.Fifo)]
    [InlineData(ModoFila.RoundRobin)]
    public void Fila_vazia_nao_tem_proximo(ModoFila modo)
    {
        Assert.Null(OrdenadorFila.ProximoItem([], modo));
    }
}
