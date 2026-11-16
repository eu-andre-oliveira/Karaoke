using Karaoke.Domain.Entities;

namespace Karaoke.Domain.Tests;

public class EventoTests
{
    [Fact]
    public void Codigo_gerado_tem_tamanho_e_alfabeto_sem_caracteres_ambiguos()
    {
        for (var i = 0; i < 200; i++)
        {
            var codigo = Evento.GerarCodigo();

            Assert.Equal(Evento.TamanhoCodigo, codigo.Length);
            Assert.All(codigo, c => Assert.DoesNotContain(c, "01OI"));
            Assert.Equal(codigo.ToUpperInvariant(), codigo);
        }
    }

    [Fact]
    public void Codigos_gerados_variam()
    {
        var codigos = Enumerable.Range(0, 100).Select(_ => Evento.GerarCodigo()).ToHashSet();

        Assert.True(codigos.Count > 95);
    }

    [Theory]
    [InlineData("abc123", "ABC123")]
    [InlineData("  kpw5b9 ", "KPW5B9")]
    [InlineData(null, "")]
    public void Normaliza_codigo_digitado(string? entrada, string esperado)
    {
        Assert.Equal(esperado, Evento.NormalizarCodigo(entrada));
    }

    [Fact]
    public void Encerrar_desativa_e_registra_a_data()
    {
        var evento = new Evento { Codigo = "ABC234", Nome = "Festa", HostUserId = "u1" };

        evento.Encerrar();

        Assert.False(evento.Ativo);
        Assert.NotNull(evento.DataEncerramento);
    }

    [Fact]
    public void Tokens_de_participante_sao_unicos_e_longos()
    {
        var tokens = Enumerable.Range(0, 50).Select(_ => new Participante { Nome = "x" }.Token).ToHashSet();

        Assert.Equal(50, tokens.Count);
        Assert.All(tokens, t => Assert.True(t.Length >= 32));
    }
}
