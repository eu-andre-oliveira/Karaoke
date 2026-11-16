using Karaoke.Domain.ValueObjects;

namespace Karaoke.Domain.Tests;

public class YouTubeUrlTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ&t=42s")]
    [InlineData("https://www.youtube.com/watch?feature=share&v=dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://music.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("  https://youtu.be/dQw4w9WgXcQ  ")]
    public void Extrai_id_de_links_validos(string url)
    {
        Assert.True(YouTubeUrl.TryGetVideoId(url, out var id));
        Assert.Equal("dQw4w9WgXcQ", id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("dQw4w9WgXcQ")]
    [InlineData("https://evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com.evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch")]
    [InlineData("https://www.youtube.com/watch?v=curto")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ<script>")]
    [InlineData("https://www.youtube.com/channel/UCxyz")]
    [InlineData("https://youtu.be/")]
    public void Rejeita_links_invalidos_ou_de_outros_sites(string? url)
    {
        Assert.False(YouTubeUrl.TryGetVideoId(url, out var id));
        Assert.Empty(id);
    }

    [Fact]
    public void Monta_urls_derivadas_a_partir_do_id()
    {
        Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", YouTubeUrl.Watch("dQw4w9WgXcQ"));
        Assert.Equal("https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg", YouTubeUrl.Thumbnail("dQw4w9WgXcQ"));
    }
}
