using System.Security.Cryptography;
using DirSync.Core.Checksum.Interfaces;

namespace DirSync.Core.Checksum.Services;

public class Sha256ChecksumService : IChecksumService
{
    public async Task<string> ChecksumAsync(Stream stream)
    {
        var sha256BytesLength = 32;
        var hashBytes = new byte[sha256BytesLength];
        using (var sha256 = SHA256.Create())
        {
            hashBytes = await sha256.ComputeHashAsync(stream);
        }

        return Convert.ToHexStringLower(hashBytes);
    }
}