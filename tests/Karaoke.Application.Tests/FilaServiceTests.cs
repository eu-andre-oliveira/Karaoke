using Karaoke.Application.Common;
using Karaoke.Application.DTOs;
using Karaoke.Application.UseCases;
using Karaoke.Domain.Entities;
using Karaoke.Domain.Enums;

namespace Karaoke.Application.Tests;

public class FilaServiceTests : IDisposable
{
    private readonly TestDb _t = new();
    private readonly FilaService _fila;
    private readonly EventoService _eventos;

    private string _hostId = null!;
    private string _codigo = null!;
    private string _tokenAna = null!;
    private string _tokenBeto = null!;

    public FilaServiceTests()
    {
        _fila = new FilaService(_t.Db, _t.Notifier);
        _eventos = new EventoService(_t.Db, _fila);
    }

    public void Dispose() => _t.Dispose();

    /// <summary>Cria host, evento ativo e dois convidados (Ana e Beto).</summary>
    private async Task PrepararAsync(ModoFila modo = ModoFila.Fifo)
    {
        _hostId = (await _t.CriarHostAsync()).Id;
        var evento = (await _eventos.CriarAsync(_hostId, new CriarEventoRequest("Festa de teste", modo))).Valor!;
        _codigo = evento.Codigo;
        _tokenAna = (await _eventos.EntrarAsync(_codigo, new EntrarEventoRequest("Ana"))).Valor!.Token;
        _tokenBeto = (await _eventos.EntrarAsync(_codigo, new EntrarEventoRequest("Beto"))).Valor!.Token;
        _t.Notifier.Envios.Clear();
    }

    private static AdicionarItemRequest Musica(string titulo, string videoId = "dQw4w9WgXcQ") =>
        new(null, titulo, null, $"https://youtu.be/{videoId}");

    private Task<Result<ItemFilaDto>> Add(string token, string titulo, string videoId = "dQw4w9WgXcQ") =>
        _fila.AdicionarAsync(_codigo, token, Musica(titulo, videoId));

    private async Task<string[]> AguardandoAsync() =>
        (await _fila.ObterAsync(_codigo)).Valor!.Aguardando.Select(i => i.Titulo).ToArray();

    // ---------- Adicionar ----------

    [Fact]
    public async Task Adiciona_musica_normaliza_link_e_avisa_clientes()
    {
        await PrepararAsync();

        var r = await _fila.AdicionarAsync(_codigo, _tokenAna, new AdicionarItemRequest(null, "Evidências", "Chitãozinho", "https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=10"));

        Assert.True(r.Sucesso);
        Assert.Equal("dQw4w9WgXcQ", r.Valor!.VideoId);
        Assert.Equal("Ana", r.Valor.Cantor);
        Assert.Contains("hqdefault", r.Valor.ThumbnailUrl);

        var envio = Assert.Single(_t.Notifier.Envios);
        Assert.Equal(_codigo, envio.Codigo);
        Assert.Single(envio.Fila.Aguardando);
    }

    [Fact]
    public async Task Rejeita_link_que_nao_e_do_youtube()
    {
        await PrepararAsync();

        var r = await Add(_tokenAna, "X", "dQw4w9WgXcQ");
        Assert.True(r.Sucesso);

        var ruim = await _fila.AdicionarAsync(_codigo, _tokenAna, new AdicionarItemRequest(null, "X", null, "https://evil.com/watch?v=dQw4w9WgXcQ"));

        Assert.False(ruim.Sucesso);
        Assert.Equal(TipoErro.Invalido, ruim.Erro!.Tipo);
    }

    [Fact]
    public async Task Exige_token_valido_de_participante_do_mesmo_evento()
    {
        await PrepararAsync();

        var semToken = await Add("", "X");
        var tokenFalso = await Add("nao-existe", "X");

        Assert.Equal(TipoErro.NaoAutorizado, semToken.Erro!.Tipo);
        Assert.Equal(TipoErro.NaoAutorizado, tokenFalso.Erro!.Tipo);
    }

    [Fact]
    public async Task Token_de_outro_evento_nao_vale()
    {
        await PrepararAsync();
        var outro = (await _eventos.CriarAsync(_hostId, new CriarEventoRequest("Outro evento"))).Valor!;

        var r = await _fila.AdicionarAsync(outro.Codigo, _tokenAna, Musica("X"));

        Assert.Equal(TipoErro.NaoAutorizado, r.Erro!.Tipo);
    }

    [Fact]
    public async Task Limita_musicas_aguardando_por_participante()
    {
        await PrepararAsync();

        for (var i = 1; i <= Evento.MaxItensAguardandoPorParticipante; i++)
            Assert.True((await Add(_tokenAna, $"A{i}")).Sucesso);

        var estourou = await Add(_tokenAna, "A-extra");
        var outraPessoa = await Add(_tokenBeto, "B1");

        Assert.Equal(TipoErro.Conflito, estourou.Erro!.Tipo);
        Assert.True(outraPessoa.Sucesso);
    }

    [Fact]
    public async Task Nao_aceita_musica_em_evento_encerrado()
    {
        await PrepararAsync();
        await _eventos.EncerrarAsync(_codigo, _hostId);

        var r = await Add(_tokenAna, "X");

        Assert.Equal(TipoErro.Conflito, r.Erro!.Tipo);
    }

    // ---------- Ordem ----------

    [Fact]
    public async Task Fifo_mantem_ordem_de_chegada()
    {
        await PrepararAsync(ModoFila.Fifo);
        await Add(_tokenAna, "A1");
        await Add(_tokenAna, "A2");
        await Add(_tokenBeto, "B1");

        Assert.Equal(new[] { "A1", "A2", "B1" }, await AguardandoAsync());
    }

    [Fact]
    public async Task Round_robin_intercala_e_trocar_modo_reordena_ao_vivo()
    {
        await PrepararAsync(ModoFila.Fifo);
        await Add(_tokenAna, "A1");
        await Add(_tokenAna, "A2");
        await Add(_tokenBeto, "B1");

        var r = await _eventos.AlterarModoAsync(_codigo, _hostId, new AlterarModoRequest(ModoFila.RoundRobin));

        Assert.True(r.Sucesso);
        Assert.Equal(new[] { "A1", "B1", "A2" }, await AguardandoAsync());
        Assert.Equal(ModoFila.RoundRobin, _t.Notifier.Envios.Last().Fila.Modo);
    }

    // ---------- Fluxo do anfitrião ----------

    [Fact]
    public async Task Fluxo_iniciar_finalizar_e_proxima()
    {
        await PrepararAsync(ModoFila.RoundRobin);
        await Add(_tokenAna, "A1");
        await Add(_tokenAna, "A2");
        await Add(_tokenBeto, "B1");

        var primeira = await _fila.IniciarProximaAsync(_codigo, _hostId);
        Assert.Equal("A1", primeira.Valor!.Titulo);
        Assert.Equal(StatusItemFila.Cantando, primeira.Valor.Status);

        // Não dá para iniciar outra enquanto uma está tocando.
        Assert.Equal(TipoErro.Conflito, (await _fila.IniciarProximaAsync(_codigo, _hostId)).Erro!.Tipo);

        Assert.True((await _fila.FinalizarAtualAsync(_codigo, _hostId)).Sucesso);

        // Ana acabou de cantar: no revezamento, agora é a vez do Beto.
        var segunda = await _fila.IniciarProximaAsync(_codigo, _hostId);
        Assert.Equal("B1", segunda.Valor!.Titulo);

        var snapshot = (await _fila.ObterAsync(_codigo)).Valor!;
        Assert.Equal("B1", snapshot.Atual!.Titulo);
        Assert.Equal(1, snapshot.TotalCantadas);
        Assert.Equal(new[] { "A2" }, snapshot.Aguardando.Select(i => i.Titulo));
    }

    [Fact]
    public async Task Pular_nao_conta_como_cantada()
    {
        await PrepararAsync();
        await Add(_tokenAna, "A1");
        await _fila.IniciarProximaAsync(_codigo, _hostId);

        await _fila.PularAtualAsync(_codigo, _hostId);

        var snapshot = (await _fila.ObterAsync(_codigo)).Valor!;
        Assert.Null(snapshot.Atual);
        Assert.Equal(0, snapshot.TotalCantadas);
    }

    [Fact]
    public async Task Fila_vazia_e_sem_musica_atual_geram_conflito()
    {
        await PrepararAsync();

        Assert.Equal(TipoErro.Conflito, (await _fila.IniciarProximaAsync(_codigo, _hostId)).Erro!.Tipo);
        Assert.Equal(TipoErro.Conflito, (await _fila.FinalizarAtualAsync(_codigo, _hostId)).Erro!.Tipo);
        Assert.Equal(TipoErro.Conflito, (await _fila.PularAtualAsync(_codigo, _hostId)).Erro!.Tipo);
    }

    [Fact]
    public async Task Somente_o_host_do_evento_controla_o_player()
    {
        await PrepararAsync();
        await Add(_tokenAna, "A1");
        var intruso = (await _t.CriarHostAsync("intruso@teste.com")).Id;

        Assert.Equal(TipoErro.NaoAutorizado, (await _fila.IniciarProximaAsync(_codigo, intruso)).Erro!.Tipo);
        Assert.Equal(TipoErro.NaoAutorizado, (await _eventos.EncerrarAsync(_codigo, intruso)).Erro!.Tipo);
        Assert.Equal(TipoErro.NaoAutorizado,
            (await _eventos.AlterarModoAsync(_codigo, intruso, new AlterarModoRequest(ModoFila.RoundRobin))).Erro!.Tipo);
    }

    // ---------- Remover ----------

    [Fact]
    public async Task Convidado_so_remove_a_propria_musica_e_host_remove_qualquer_uma()
    {
        await PrepararAsync();
        var daAna = (await Add(_tokenAna, "A1")).Valor!.Id;
        var daAna2 = (await Add(_tokenAna, "A2")).Valor!.Id;

        var beto = await _fila.RemoverAsync(_codigo, daAna, Ator.Convidado(_tokenBeto));
        var ana = await _fila.RemoverAsync(_codigo, daAna, Ator.Convidado(_tokenAna));
        var host = await _fila.RemoverAsync(_codigo, daAna2, Ator.Host(_hostId));

        Assert.Equal(TipoErro.NaoAutorizado, beto.Erro!.Tipo);
        Assert.True(ana.Sucesso);
        Assert.True(host.Sucesso);
        Assert.Empty(await AguardandoAsync());
    }

    [Fact]
    public async Task Nao_remove_musica_que_ja_esta_cantando()
    {
        await PrepararAsync();
        var id = (await Add(_tokenAna, "A1")).Valor!.Id;
        await _fila.IniciarProximaAsync(_codigo, _hostId);

        var r = await _fila.RemoverAsync(_codigo, id, Ator.Host(_hostId));

        Assert.Equal(TipoErro.Conflito, r.Erro!.Tipo);
    }

    // ---------- Biblioteca ----------

    [Fact]
    public async Task Adiciona_da_biblioteca_e_conta_uso_mas_ignora_bloqueadas()
    {
        await PrepararAsync();
        var autor = _hostId;
        var ok = new MusicaBiblioteca { Titulo = "Boa", UrlStreaming = "https://youtu.be/9bZkp7q19f0", CriadoPorUsuarioId = autor };
        var bloqueada = new MusicaBiblioteca { Titulo = "Ruim", UrlStreaming = "https://youtu.be/kJQP7kiw5Fk", CriadoPorUsuarioId = autor, Bloqueada = true };
        _t.Db.MusicasBiblioteca.AddRange(ok, bloqueada);
        await _t.Db.SaveChangesAsync();

        var r1 = await _fila.AdicionarAsync(_codigo, _tokenAna, new AdicionarItemRequest(ok.Id, null, null, null));
        var r2 = await _fila.AdicionarAsync(_codigo, _tokenAna, new AdicionarItemRequest(bloqueada.Id, null, null, null));

        Assert.True(r1.Sucesso);
        Assert.Equal("Boa", r1.Valor!.Titulo);
        Assert.Equal(TipoErro.NaoEncontrado, r2.Erro!.Tipo);
        Assert.Equal(1, _t.Db.MusicasBiblioteca.Single(m => m.Id == ok.Id).QuantidadeUsos);
    }

    // ---------- Evento ----------

    [Fact]
    public async Task Entrar_valida_nome_e_evento()
    {
        await PrepararAsync();

        Assert.Equal(TipoErro.Invalido, (await _eventos.EntrarAsync(_codigo, new EntrarEventoRequest("  "))).Erro!.Tipo);
        Assert.Equal(TipoErro.NaoEncontrado, (await _eventos.EntrarAsync("ZZZZZZ", new EntrarEventoRequest("Ana"))).Erro!.Tipo);

        await _eventos.EncerrarAsync(_codigo, _hostId);
        Assert.Equal(TipoErro.Conflito, (await _eventos.EntrarAsync(_codigo, new EntrarEventoRequest("Tarde"))).Erro!.Tipo);
    }

    [Fact]
    public async Task Codigo_e_aceito_em_minusculas()
    {
        await PrepararAsync();

        var r = await _eventos.ObterAsync(_codigo.ToLowerInvariant());

        Assert.True(r.Sucesso);
    }

    [Fact]
    public async Task Criar_evento_valida_nome()
    {
        var host = await _t.CriarHostAsync();

        var r = await _eventos.CriarAsync(host.Id, new CriarEventoRequest("ab"));

        Assert.Equal(TipoErro.Invalido, r.Erro!.Tipo);
    }
}
