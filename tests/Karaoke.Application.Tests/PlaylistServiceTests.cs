using Karaoke.Application.Common;
using Karaoke.Application.DTOs;
using Karaoke.Application.UseCases;

namespace Karaoke.Application.Tests;

public class PlaylistServiceTests : IDisposable
{
    private readonly TestDb _t = new();
    private readonly PlaylistService _svc;

    public PlaylistServiceTests() => _svc = new PlaylistService(_t.Db);

    public void Dispose() => _t.Dispose();

    private static AdicionarMusicaRequest Nova(string titulo, string url = "https://youtu.be/dQw4w9WgXcQ") =>
        new(titulo, "Artista", null, url, null);

    [Fact]
    public async Task Cria_e_lista_somente_as_playlists_do_proprio_usuario()
    {
        var ana = (await _t.CriarHostAsync("ana@teste.com")).Id;
        var beto = (await _t.CriarHostAsync("beto@teste.com")).Id;

        await _svc.CriarAsync(ana, new SalvarPlaylistRequest("Sertanejo", null));
        await _svc.CriarAsync(beto, new SalvarPlaylistRequest("Rock", null));

        var daAna = await _svc.ListarAsync(ana);

        Assert.Equal("Sertanejo", Assert.Single(daAna).Nome);
    }

    [Fact]
    public async Task Nao_acessa_nem_altera_playlist_de_outro_usuario()
    {
        var ana = (await _t.CriarHostAsync("ana@teste.com")).Id;
        var beto = (await _t.CriarHostAsync("beto@teste.com")).Id;
        var id = (await _svc.CriarAsync(ana, new SalvarPlaylistRequest("Minha", null))).Valor!.Id;

        Assert.Equal(TipoErro.NaoEncontrado, (await _svc.ObterAsync(beto, id)).Erro!.Tipo);
        Assert.Equal(TipoErro.NaoEncontrado, (await _svc.AtualizarAsync(beto, id, new SalvarPlaylistRequest("Roubada", null))).Erro!.Tipo);
        Assert.Equal(TipoErro.NaoEncontrado, (await _svc.ExcluirAsync(beto, id)).Erro!.Tipo);
        Assert.Equal(TipoErro.NaoEncontrado, (await _svc.AdicionarMusicaAsync(beto, id, Nova("X"))).Erro!.Tipo);
    }

    [Fact]
    public async Task Valida_nome_da_playlist()
    {
        var ana = (await _t.CriarHostAsync()).Id;

        Assert.Equal(TipoErro.Invalido, (await _svc.CriarAsync(ana, new SalvarPlaylistRequest(" ", null))).Erro!.Tipo);
        Assert.Equal(TipoErro.Invalido, (await _svc.CriarAsync(ana, new SalvarPlaylistRequest(new string('x', 201), null))).Erro!.Tipo);
    }

    [Fact]
    public async Task Musica_nova_alimenta_a_biblioteca_e_repetida_so_conta_uso()
    {
        var ana = (await _t.CriarHostAsync()).Id;
        var id = (await _svc.CriarAsync(ana, new SalvarPlaylistRequest("P", null))).Valor!.Id;

        await _svc.AdicionarMusicaAsync(ana, id, Nova("Evidências"));
        await _svc.AdicionarMusicaAsync(ana, id, Nova("Evidências (de novo)"));

        var biblioteca = Assert.Single(_t.Db.MusicasBiblioteca);
        Assert.Equal(2, biblioteca.QuantidadeUsos);
        Assert.Equal(2, (await _svc.ObterAsync(ana, id)).Valor!.Musicas.Count);
    }

    [Fact]
    public async Task Ordem_incrementa_e_reordenar_aplica_a_nova_sequencia()
    {
        var ana = (await _t.CriarHostAsync()).Id;
        var id = (await _svc.CriarAsync(ana, new SalvarPlaylistRequest("P", null))).Valor!.Id;
        var m1 = (await _svc.AdicionarMusicaAsync(ana, id, Nova("Um", "https://youtu.be/dQw4w9WgXcQ"))).Valor!.Id;
        var m2 = (await _svc.AdicionarMusicaAsync(ana, id, Nova("Dois", "https://youtu.be/9bZkp7q19f0"))).Valor!.Id;
        var m3 = (await _svc.AdicionarMusicaAsync(ana, id, Nova("Tres", "https://youtu.be/kJQP7kiw5Fk"))).Valor!.Id;

        await _svc.ReordenarAsync(ana, id, new ReordenarMusicasRequest([m3, m1, m2]));

        var titulos = (await _svc.ObterAsync(ana, id)).Valor!.Musicas.Select(m => m.Titulo);
        Assert.Equal(new[] { "Tres", "Um", "Dois" }, titulos);
    }

    [Fact]
    public async Task Marca_como_cantada_e_remove_musica()
    {
        var ana = (await _t.CriarHostAsync()).Id;
        var id = (await _svc.CriarAsync(ana, new SalvarPlaylistRequest("P", null))).Valor!.Id;
        var musica = (await _svc.AdicionarMusicaAsync(ana, id, Nova("Um"))).Valor!.Id;

        await _svc.MarcarCantadaAsync(ana, id, musica, new MarcarCantadaRequest(true));
        Assert.True((await _svc.ObterAsync(ana, id)).Valor!.Musicas.Single().JaCantada);

        Assert.True((await _svc.RemoverMusicaAsync(ana, id, musica)).Sucesso);
        Assert.Empty((await _svc.ObterAsync(ana, id)).Valor!.Musicas);
    }

    [Fact]
    public async Task Adicionar_da_biblioteca_ignora_bloqueadas()
    {
        var ana = (await _t.CriarHostAsync()).Id;
        var id = (await _svc.CriarAsync(ana, new SalvarPlaylistRequest("P", null))).Valor!.Id;
        var bloqueada = new Karaoke.Domain.Entities.MusicaBiblioteca
        {
            Titulo = "Ruim", UrlStreaming = "https://youtu.be/dQw4w9WgXcQ", CriadoPorUsuarioId = ana, Bloqueada = true
        };
        _t.Db.MusicasBiblioteca.Add(bloqueada);
        await _t.Db.SaveChangesAsync();

        var r = await _svc.AdicionarDaBibliotecaAsync(ana, id, new AdicionarDaBibliotecaRequest(bloqueada.Id, null));

        Assert.Equal(TipoErro.NaoEncontrado, r.Erro!.Tipo);
    }
}
