namespace DirSync.Core.DirectoryScanner.Models;

public record DirectorySnapshot
{
    public required string RootDirectoryPath { get; init; }
    public required HashSet<string> Directories { get; init; }
    public required Dictionary<string, FileSnapshot> Files { get; init; }

}