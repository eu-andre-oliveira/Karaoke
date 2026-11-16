using Karaoke.Application.DTOs;
using Karaoke.Application.Interfaces;
using Karaoke.Domain.Entities;
using Karaoke.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Karaoke.Application.Tests;

/// <summary>Sqlite em memória com o schema real das migrations. Um banco novo por teste.</summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    public KaraokeDbContext Db { get; }
    public FakeNotifier Notifier { get; } = new();

    public TestDb()
    {
        _conexao.Open();
        Db = new KaraokeDbContext(new DbContextOptionsBuilder<KaraokeDbContext>().UseSqlite(_conexao).Options);
        Db.Database.Migrate();
    }

    public async Task<ApplicationUser> CriarHostAsync(string email = "host@teste.com")
    {
        var user = new ApplicationUser { UserName = email, Email = email, NomeCompleto = "Host" };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    public void Dispose()
    {
        Db.Dispose();
        _conexao.Dispose();
    }
}

public sealed class FakeNotifier : IFilaNotifier
{
    public List<(string Codigo, FilaDto Fila)> Envios { get; } = new();

    public Task FilaAtualizadaAsync(string codigoEvento, FilaDto fila, CancellationToken cancellationToken = default)
    {
        Envios.Add((codigoEvento, fila));
        return Task.CompletedTask;
    }
}
