using System.Text.RegularExpressions;

namespace Karaoke.Domain.ValueObjects;

/// <summary>Extrai o id do vídeo de links do YouTube e monta URLs derivadas.</summary>
public static partial class YouTubeUrl
{
    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex IdRegex();

    private static readonly string[] HostsValidos =
    [
        "youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com",
        "youtu.be", "www.youtu.be", "youtube-nocookie.com", "www.youtube-nocookie.com"
    ];

    public static bool TryGetVideoId(string? url, out string videoId)
    {
        videoId = string.Empty;
        if (string.IsNullOrWhiteSpace(url)) return false;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme is not ("http" or "https")) return false;

        var host = uri.Host.ToLowerInvariant();
        if (!HostsValidos.Contains(host)) return false;

        string? candidato = null;

        if (host.EndsWith("youtu.be", StringComparison.Ordinal))
        {
            candidato = uri.AbsolutePath.Trim('/').Split('/')[0];
        }
        else
        {
            var segmentos = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segmentos.Length >= 2 && segmentos[0] is "embed" or "shorts" or "v" or "live")
                candidato = segmentos[1];
            else if (segmentos.Length == 1 && segmentos[0] == "watch")
                candidato = ObterParametro(uri.Query, "v");
        }

        if (candidato is null || !IdRegex().IsMatch(candidato)) return false;

        videoId = candidato;
        return true;
    }

    public static string Thumbnail(string videoId) => $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";

    public static string Watch(string videoId) => $"https://www.youtube.com/watch?v={videoId}";

    private static string? ObterParametro(string query, string nome)
    {
        foreach (var par in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = par.Split('=', 2);
            if (kv.Length == 2 && kv[0] == nome) return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }
}
