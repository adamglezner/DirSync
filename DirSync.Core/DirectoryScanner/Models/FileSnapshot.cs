namespace DirSync.Core.DirectoryScanner.Models;

public record FileSnapshot
{
    public required string Checksum { get; init; }
    public required long Size { get; init; }
}