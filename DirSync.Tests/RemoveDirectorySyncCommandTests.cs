using DirSync.Core.SyncCommands;

namespace DirSync.Tests;

public class RemoveDirectorySyncCommandTests
{
    private string _tempDirPath;

    [SetUp]
    public void Setup()
    {
        _tempDirPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirPath);
        Directory.CreateDirectory(Path.Combine(_tempDirPath, "empty_directory"));
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
    public async Task ExecuteAsync_RemovesDirectory()
    {
        await new RemoveDirectorySyncCommand(Path.Combine(_tempDirPath, "empty_directory")).ExecuteAsync();
        Assert.That(Directory.Exists(Path.Combine(_tempDirPath, "empty_directory")), Is.False);
    }
}