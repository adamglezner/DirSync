namespace DirSync.Tests;

using DirSync.Core.Checksum.Interfaces;
using DirSync.Core.DirectoryScanner.Models;
using DirSync.Core.DirectoryScanner.Services;
using Moq;

public class DirectoryScannerServiceTests
{
    private DirectoryScannerService _directoryScannerService;
    private string _tempDirPath;

    [SetUp]
    public void Setup()
    {
        var mockChecksumService = new Mock<IChecksumService>();
        mockChecksumService
            .Setup(mock => mock.ChecksumAsync(It.IsAny<Stream>()))
            .ReturnsAsync("checksum");

        _directoryScannerService = new DirectoryScannerService(mockChecksumService.Object);

        _tempDirPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirPath);
        Directory.CreateDirectory(Path.Combine(_tempDirPath, "subdirectory_empty"));
        Directory.CreateDirectory(Path.Combine(_tempDirPath, "subdirectory_with_files"));
        File.WriteAllText(Path.Combine(_tempDirPath, "test.txt"), string.Empty);
        File.WriteAllText(Path.Combine(_tempDirPath, "subdirectory_with_files", "test1.txt"), string.Empty);
        File.WriteAllText(Path.Combine(_tempDirPath, "subdirectory_with_files", "test2.txt"), string.Empty);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirPath))
        {
            Directory.Delete(_tempDirPath, recursive: true);
        }
    }

    [Test]
    public async Task ScanAsync_WithKnownInput_ReturnsExpectedResult()
    {
        var expected = new DirectorySnapshot
        {
            RootDirectoryPath = _tempDirPath,
            Directories = ["subdirectory_empty", "subdirectory_with_files"],
            Files = new Dictionary<string, FileSnapshot>
            {
                ["test.txt"] = new FileSnapshot { Size = 0, Checksum = "checksum" },
                [Path.Combine("subdirectory_with_files", "test1.txt")] = new FileSnapshot { Size = 0, Checksum = "checksum" },
                [Path.Combine("subdirectory_with_files", "test2.txt")] = new FileSnapshot { Size = 0, Checksum = "checksum" },
            }
        };

        var returned = await _directoryScannerService.ScanAsync(_tempDirPath);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(returned.RootDirectoryPath, Is.EqualTo(expected.RootDirectoryPath));
            Assert.That(returned.Directories, Is.EquivalentTo(expected.Directories));
            Assert.That(returned.Files.Keys, Is.EquivalentTo(expected.Files.Keys));

            foreach (var key in expected.Files.Keys)
            {
                Assert.That(returned.Files[key], Is.EqualTo(expected.Files[key]));
            }
        }
    }
}
