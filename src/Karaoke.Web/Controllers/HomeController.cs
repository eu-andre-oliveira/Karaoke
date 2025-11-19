using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Karaoke.Infrastructure.Data;
using Karaoke.Web.Models;
using System.Security.Claims;

namespace Karaoke.Web.Controllers;

public class HomeController : Controller
{
    private readonly KaraokeDbContext _context;

    public HomeController(KaraokeDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            return View(new List<PlaylistCardViewModel>());
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var playlists = await _context.Playlists
            .Where(p => p.UsuarioId == userId)
            .Include(p => p.Musicas)
            .OrderByDescending(p => p.DataCriacao)
            .Select(p => new PlaylistCardViewModel
            {
                Id = p.Id,
                Nome = p.Nome,
                Descricao = p.Descricao,
                TotalMusicas = p.Musicas.Count,
                PrimeirasMusicasTitulos = p.Musicas
                    .OrderBy(m => m.Id)
                    .Take(3)
                    .Select(m => m.Titulo)
                    .ToList(),
                DataCriacao = p.DataCriacao
            })
            .ToListAsync();

        return View(playlists);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
