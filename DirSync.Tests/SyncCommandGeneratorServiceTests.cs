using DirSync.Core.DirectoryScanner.Models;
using DirSync.Core.SyncCommands.Enums;
using DirSync.Core.SyncCommands.Services;

namespace DirSync.Tests;

public class SyncCommandGeneratorServiceTests
{
    private SyncCommandGeneratorService _syncCommandGeneratorService;
    [SetUp]
    public void Setup()
    {
        _syncCommandGeneratorService = new SyncCommandGeneratorService();
    }

    [Test]
    public void GenerateSyncCommands_WithDirectoryExistingOnlyInSource_ReturnsAddDirectoryCommand()
    {
        var source = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "source"),
            Directories = ["subdirectory_empty"],
            Files = [],
        };
        var replica = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "replica"),
            Directories = [],
            Files = [],
        };

        var returned = _syncCommandGeneratorService.GenerateSyncCommands(source, replica);
        var generatedCommands = returned
        .SelectMany(kvp => kvp.Value)
        .Select(cmd => string.Join(" ", cmd.DryRun()))
        .ToList();

        var expectedCommands = new List<string> { $"{nameof(SyncCommandTypeEnum.AddDirectory)} {Path.Combine(Path.GetTempPath(), "replica", "subdirectory_empty")}" };

        Assert.That(generatedCommands, Is.EqualTo(expectedCommands));
    }

    [Test]
    public void GenerateSyncCommands_WithDirectoryExistingOnlyInReplica_ReturnsRemoveDirectoryCommand()
    {
        var source = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "source"),
            Directories = [],
            Files = [],
        };
        var replica = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "replica"),
            Directories = ["subdirectory_empty"],
            Files = [],
        };

        var returned = _syncCommandGeneratorService.GenerateSyncCommands(source, replica);
        var generatedCommands = returned
        .SelectMany(kvp => kvp.Value)
        .Select(cmd => string.Join(" ", cmd.DryRun()))
        .ToList();

        var expectedCommands = new List<string> { $"{nameof(SyncCommandTypeEnum.RemoveDirectory)} {Path.Combine(Path.GetTempPath(), "replica", "subdirectory_empty")}" };

        Assert.That(generatedCommands, Is.EqualTo(expectedCommands));
    }

    [Test]
    public void GenerateSyncCommands_WithFileExistingOnlyInSource_ReturnsAddFileCommand()
    {
        var source = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "source"),
            Directories = [],
            Files = new Dictionary<string, FileSnapshot>
            {
                ["test.txt"] = new FileSnapshot { Size = 0, Checksum = "checksum" },
            },
        };
        var replica = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "replica"),
            Directories = [],
            Files = [],
        };

        var returned = _syncCommandGeneratorService.GenerateSyncCommands(source, replica);
        var generatedCommands = returned
        .SelectMany(kvp => kvp.Value)
        .Select(cmd => string.Join(" ", cmd.DryRun()))
        .ToList();

        var expectedCommands = new List<string> { $"{nameof(SyncCommandTypeEnum.AddFile)} {Path.Combine(Path.GetTempPath(), "source", "test.txt")} {Path.Combine(Path.GetTempPath(), "replica", "test.txt")}" };

        Assert.That(generatedCommands, Is.EqualTo(expectedCommands));
    }

    [Test]
    public void GenerateSyncCommands_WithFileExistingOnlyInReplica_ReturnsRemoveFileCommand()
    {
        var source = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "source"),
            Directories = [],
            Files = [],
        };
        var replica = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "replica"),
            Directories = [],
            Files = new Dictionary<string, FileSnapshot>
            {
                ["test.txt"] = new FileSnapshot { Size = 0, Checksum = "checksum" },
            },
        };

        var returned = _syncCommandGeneratorService.GenerateSyncCommands(source, replica);
        var generatedCommands = returned
        .SelectMany(kvp => kvp.Value)
        .Select(cmd => string.Join(" ", cmd.DryRun()))
        .ToList();

        var expectedCommands = new List<string> { $"{nameof(SyncCommandTypeEnum.RemoveFile)} {Path.Combine(Path.GetTempPath(), "replica", "test.txt")}" };

        Assert.That(generatedCommands, Is.EqualTo(expectedCommands));
    }

    [Test]
    public void GenerateSyncCommands_WhenChecksumsDiffer_ReturnsRemoveAddFileCommands()
    {
        var source = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "source"),
            Directories = [],
            Files = new Dictionary<string, FileSnapshot>
            {
                ["test.txt"] = new FileSnapshot { Size = 0, Checksum = "checksum" },
            },
        };
        var replica = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "replica"),
            Directories = [],
            Files = new Dictionary<string, FileSnapshot>
            {
                ["test.txt"] = new FileSnapshot { Size = 0, Checksum = "checksum2" },
            },
        };

        var returned = _syncCommandGeneratorService.GenerateSyncCommands(source, replica);
        var generatedCommands = returned
        .SelectMany(kvp => kvp.Value)
        .Select(cmd => string.Join(" ", cmd.DryRun()))
        .ToList()
        .OrderBy(s => s.Length);

        var expectedCommands = new List<string> {
            $"{nameof(SyncCommandTypeEnum.AddFile)} {Path.Combine(Path.GetTempPath(), "source", "test.txt")} {Path.Combine(Path.GetTempPath(), "replica", "test.txt")}",
            $"{nameof(SyncCommandTypeEnum.RemoveFile)} {Path.Combine(Path.GetTempPath(), "replica", "test.txt")}",
        }.OrderBy(s => s.Length);

        Assert.That(generatedCommands, Is.EqualTo(expectedCommands));
    }

    [Test]
    public void GenerateSyncCommands_WhenDirectoriesMatch_ReturnsNoCommands()
    {
        var source = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "source"),
            Directories = ["empty_directory"],
            Files = new Dictionary<string, FileSnapshot>
            {
                ["test.txt"] = new FileSnapshot { Size = 0, Checksum = "checksum" },
            },
        };
        var replica = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "replica"),
            Directories = ["empty_directory"],
            Files = new Dictionary<string, FileSnapshot>
            {
                ["test.txt"] = new FileSnapshot { Size = 0, Checksum = "checksum" },
            },
        };

        var returned = _syncCommandGeneratorService.GenerateSyncCommands(source, replica);
        var generatedCommands = returned
        .SelectMany(kvp => kvp.Value)
        .Select(cmd => string.Join(" ", cmd.DryRun()))
        .ToList();

        var expectedCommands = new List<string>();

        Assert.That(generatedCommands, Is.EqualTo(expectedCommands));
    }
    
    [Test]
    public void GenerateSyncCommands_WhenBothDirectoriesEmpty_ReturnsNoCommands()
    {
        var source = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "source"),
            Directories = [],
            Files = [],
        };
        var replica = new DirectorySnapshot
        {
            RootDirectoryPath = Path.Combine(Path.GetTempPath(), "replica"),
            Directories = [],
            Files = [],
        };

        var returned = _syncCommandGeneratorService.GenerateSyncCommands(source, replica);
        var generatedCommands = returned
        .SelectMany(kvp => kvp.Value)
        .Select(cmd => string.Join(" ", cmd.DryRun()))
        .ToList();

        var expectedCommands = new List<string>();

        Assert.That(generatedCommands, Is.EqualTo(expectedCommands));
    }
}
