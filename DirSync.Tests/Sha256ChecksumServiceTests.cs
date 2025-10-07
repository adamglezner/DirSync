using System.Text;
using DirSync.Core.Checksum.Services;

namespace DirSync.Tests;
public class Sha256ChecksumServiceTests
{
    private Sha256ChecksumService _sha256ChecksumService;
    [SetUp]
    public void Setup()
    {
        _sha256ChecksumService = new Sha256ChecksumService();
    }

    [Test]
    public async Task ChecksumAsync_WithKnownInput_ReturnsExpectedResult()
    {
        var expected = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";
        using var knownInput = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        var returned = await _sha256ChecksumService.ChecksumAsync(knownInput);
        Assert.That(returned, Is.EqualTo(expected));
    }
}
