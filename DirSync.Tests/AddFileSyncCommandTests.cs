using DirSync.Core.SyncCommands;
using System.Text;

namespace DirSync.Tests;

public class AddFileSyncCommandTests
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
    public async Task ExecuteAsync_CopiesFile()
    {
        await new AddFileSyncCommand
        (
            Path.Combine(_tempDirPath, "test.txt"),
            Path.Combine(_tempDirPath, "test_copied.txt"),
            Encoding.UTF8.GetByteCount("test")
        )
        .ExecuteAsync();

        var fileExists = File.Exists(Path.Combine(_tempDirPath, "test_copied.txt"));
        Assert.That(fileExists, Is.True);

        var copiedString = Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(_tempDirPath, "test_copied.txt")));
        Assert.That(copiedString, Is.EqualTo("test"));
    }
}