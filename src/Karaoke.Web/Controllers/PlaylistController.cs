using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Karaoke.Infrastructure.Data;
using Karaoke.Domain.Entities;
using Karaoke.Web.Models;
using System.Security.Claims;

namespace Karaoke.Web.Controllers;

[Authorize]
public class PlaylistController : Controller
{
    private readonly KaraokeDbContext _context;

    public PlaylistController(KaraokeDbContext context)
    {
        _context = context;
    }

    // GET: Playlist/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Playlist/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePlaylistViewModel model)
    {
        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var playlist = new Playlist
            {
                Nome = model.Nome,
                Descricao = model.Descricao,
                UsuarioId = userId!,
                DataCriacao = DateTime.UtcNow
            };

            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Playlist criada com sucesso!";
            return RedirectToAction("Details", new { id = playlist.Id });
        }

        return View(model);
    }

    // GET: Playlist/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var playlist = await _context.Playlists
            .Include(p => p.Musicas)
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == userId);

        if (playlist == null)
        {
            return NotFound();
        }

        var viewModel = new PlaylistDetailsViewModel
        {
            Id = playlist.Id,
            Nome = playlist.Nome,
            Descricao = playlist.Descricao,
            DataCriacao = playlist.DataCriacao,
            TotalMusicas = playlist.Musicas.Count,
            Musicas = playlist.Musicas
                .OrderBy(m => m.Ordem)
                .Select(m => new MusicaViewModel
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Artista = m.Artista,
                    Cantor = m.Cantor, // Inclua o cantor
                    UrlStreaming = m.UrlStreaming,
                    ThumbnailUrl = m.ThumbnailUrl
                }).ToList()
        };

        return View(viewModel);
    }

    // GET: Playlist/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var playlist = await _context.Playlists
            .Include(p => p.Musicas)
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == userId);

        if (playlist == null)
        {
            return NotFound();
        }

        var viewModel = new EditPlaylistViewModel
        {
            Id = playlist.Id,
            Nome = playlist.Nome,
            Descricao = playlist.Descricao,
            Musicas = playlist.Musicas
                .OrderBy(m => m.Ordem)
                .Select(m => new MusicaViewModel
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Artista = m.Artista,
                    Cantor = m.Cantor, // Inclua o cantor
                    UrlStreaming = m.UrlStreaming,
                    ThumbnailUrl = m.ThumbnailUrl
                }).ToList()
        };

        return View(viewModel);
    }

    // POST: Playlist/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditPlaylistViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == userId);

            if (playlist == null)
            {
                return NotFound();
            }

            playlist.Nome = model.Nome;
            playlist.Descricao = model.Descricao;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Playlist atualizada com sucesso!";
            return RedirectToAction("Details", new { id = playlist.Id });
        }

        return View(model);
    }

    // POST: Playlist/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var playlist = await _context.Playlists
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == userId);

        if (playlist == null)
        {
            return NotFound();
        }

        _context.Playlists.Remove(playlist);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Playlist excluída com sucesso!";
        return RedirectToAction("Index", "Home");
    }

    // GET: Playlist/AddMusica/5
    [HttpGet]
    public async Task<IActionResult> AddMusica(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var playlist = await _context.Playlists
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == userId);

        if (playlist == null)
        {
            return NotFound();
        }

        var viewModel = new AddMusicaViewModel
        {
            PlaylistId = playlist.Id,
            PlaylistNome = playlist.Nome
        };

        return View(viewModel);
    }

    // POST: Playlist/AddMusica
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMusica(AddMusicaViewModel model)
    {
        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.Id == model.PlaylistId && p.UsuarioId == userId);

            if (playlist == null)
            {
                return NotFound();
            }

            // Verificar se já existe na biblioteca
            var musicaBiblioteca = await _context.MusicasBiblioteca
                .FirstOrDefaultAsync(m => m.UrlStreaming == model.UrlStreaming && !m.Bloqueada);

            if (musicaBiblioteca == null)
            {
                // Adicionar à biblioteca compartilhada
                musicaBiblioteca = new MusicaBiblioteca
                {
                    Titulo = model.Titulo,
                    Artista = model.Artista,
                    UrlStreaming = model.UrlStreaming,
                    ThumbnailUrl = model.ThumbnailUrl,
                    CriadoPorUsuarioId = userId!,
                    QuantidadeUsos = 1,
                    Bloqueada = false
                };
                _context.MusicasBiblioteca.Add(musicaBiblioteca);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Incrementar contador
                musicaBiblioteca.QuantidadeUsos++;
            }

            // Definir a ordem como o último da lista
            int ordem = await _context.Musicas
                .Where(m => m.PlaylistId == model.PlaylistId)
                .Select(m => (int?)m.Ordem)
                .MaxAsync() ?? 0;

            var musica = new Musica
            {
                Titulo = model.Titulo,
                Artista = model.Artista,
                Cantor = model.Cantor,
                UrlStreaming = model.UrlStreaming,
                ThumbnailUrl = model.ThumbnailUrl,
                PlaylistId = model.PlaylistId,
                JaCantada = false, // Sempre inicia como não cantada
                Ordem = ordem + 1
            };

            _context.Musicas.Add(musica);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Música adicionada com sucesso!";
            return RedirectToAction("Details", new { id = model.PlaylistId });
        }

        return View(model);
    }

    // POST: Playlist/RemoveMusica
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMusica(int id, int playlistId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var playlist = await _context.Playlists
            .FirstOrDefaultAsync(p => p.Id == playlistId && p.UsuarioId == userId);

        if (playlist == null)
        {
            return NotFound();
        }

        var musica = await _context.Musicas
            .FirstOrDefaultAsync(m => m.Id == id && m.PlaylistId == playlistId);

        if (musica == null)
        {
            return NotFound();
        }

        _context.Musicas.Remove(musica);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Música removida com sucesso!";
        return RedirectToAction("Details", new { id = playlistId });
    }

    // POST: Playlist/ReorderMusicas
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderMusicas([FromBody] ReorderMusicasRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var playlist = await _context.Playlists
            .Include(p => p.Musicas)
            .FirstOrDefaultAsync(p => p.Id == request.PlaylistId && p.UsuarioId == userId);

        if (playlist == null) return NotFound();

        for (int i = 0; i < request.Ids.Count; i++)
        {
            var musica = playlist.Musicas.FirstOrDefault(m => m.Id == request.Ids[i]);
            if (musica != null) musica.Ordem = i;
        }
        await _context.SaveChangesAsync();
        return Ok();
    }

    public class ReorderMusicasRequest
    {
        public int PlaylistId { get; set; }
        public List<int> Ids { get; set; } = new();
    }

    // POST: Playlist/ToggleCantada
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCantada([FromBody] ToggleCantadaRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var musica = await _context.Musicas
            .Include(m => m.Playlist)
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.Playlist.UsuarioId == userId);

        if (musica == null) return NotFound();

        musica.JaCantada = request.Checked;
        await _context.SaveChangesAsync();
        return Ok();
    }

    public class ToggleCantadaRequest
    {
        public int Id { get; set; }
        public bool Checked { get; set; }
    }

    // GET: Playlist/SearchMusicaBiblioteca?term=evidencias
    [HttpGet]
    public async Task<IActionResult> SearchMusicaBiblioteca(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return Json(new List<MusicaBibliotecaSearchViewModel>());
        }

        var results = await _context.MusicasBiblioteca
            .Where(m => !m.Bloqueada && 
                       (m.Titulo.Contains(term) || 
                        (m.Artista != null && m.Artista.Contains(term))))
            .OrderByDescending(m => m.QuantidadeUsos)
            .Take(10)
            .Select(m => new MusicaBibliotecaSearchViewModel
            {
                Id = m.Id,
                Titulo = m.Titulo,
                Artista = m.Artista,
                UrlStreaming = m.UrlStreaming,
                ThumbnailUrl = m.ThumbnailUrl,
                QuantidadeUsos = m.QuantidadeUsos
            })
            .ToListAsync();

        return Json(results);
    }

    // POST: Playlist/AddMusicaFromBiblioteca
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMusicaFromBiblioteca(int bibliotecaId, int playlistId, string? cantor)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var playlist = await _context.Playlists
            .FirstOrDefaultAsync(p => p.Id == playlistId && p.UsuarioId == userId);

        if (playlist == null)
        {
            return NotFound();
        }

        var musicaBiblioteca = await _context.MusicasBiblioteca
            .FirstOrDefaultAsync(m => m.Id == bibliotecaId && !m.Bloqueada);

        if (musicaBiblioteca == null)
        {
            return NotFound();
        }

        // Incrementar contador de usos
        musicaBiblioteca.QuantidadeUsos++;

        // Definir a ordem como o último da lista
        int ordem = await _context.Musicas
            .Where(m => m.PlaylistId == playlistId)
            .Select(m => (int?)m.Ordem)
            .MaxAsync() ?? 0;

        var musica = new Musica
        {
            Titulo = musicaBiblioteca.Titulo,
            Artista = musicaBiblioteca.Artista,
            Cantor = cantor,
            UrlStreaming = musicaBiblioteca.UrlStreaming,
            ThumbnailUrl = musicaBiblioteca.ThumbnailUrl,
            PlaylistId = playlistId,
            MusicaBibliotecaId = bibliotecaId,
            JaCantada = false,
            Ordem = ordem + 1
        };

        _context.Musicas.Add(musica);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    // POST: Playlist/ReportMusicaBiblioteca
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportMusicaBiblioteca(int id, string motivo)
    {
        var musicaBiblioteca = await _context.MusicasBiblioteca
            .FirstOrDefaultAsync(m => m.Id == id);

        if (musicaBiblioteca == null)
        {
            return NotFound();
        }

        // Aqui você pode implementar lógica de denúncia
        // Por exemplo, criar uma tabela de denúncias e após X denúncias, bloquear automaticamente
        
        TempData["SuccessMessage"] = "Música denunciada com sucesso. Nosso time irá avaliar.";
        return RedirectToAction("AddMusica", new { id = Request.Form["playlistId"] });
    }
}