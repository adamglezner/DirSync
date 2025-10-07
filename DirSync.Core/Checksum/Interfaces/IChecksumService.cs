namespace DirSync.Core.Checksum.Interfaces;

public interface IChecksumService
{
    public Task<string> ChecksumAsync(Stream stream);
}