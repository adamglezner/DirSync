using DirSync.Core.SyncCommands;

namespace DirSync.Tests;

public class RemoveFileSyncCommandTests
{
    private string _tempDirPath;

    [SetUp]
    public void Setup()
    {
        _tempDirPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirPath);
        File.WriteAllText(Path.Combine(_tempDirPath, "test.txt"), "test");
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
    public async Task ExecuteAsync_RemovesFile()
    {
        await new RemoveFileSyncCommand(Path.Combine(_tempDirPath, "test.txt")).ExecuteAsync();

        var fileExists = File.Exists(Path.Combine(_tempDirPath, "test.txt"));
        Assert.That(fileExists, Is.False);
    }
}