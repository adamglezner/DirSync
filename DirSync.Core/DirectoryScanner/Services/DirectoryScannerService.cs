using DirSync.Core.DirectoryScanner.Models;
using DirSync.Core.Checksum.Interfaces;

namespace DirSync.Core.DirectoryScanner.Services;

public class DirectoryScannerService(IChecksumService checksumService)
{
    // the actual implementation of checksum should be easily swappable
    // we shouldn't depend on one hardcoded algorithm
    private readonly IChecksumService _checksumService = checksumService;

    public async Task<DirectorySnapshot> ScanAsync(string path)
    {
        // for simplicity sake of this example
        // we just do the whole task at once
        // this could be parallelized or at least concurrent
        // or done by depth first which may or may not have better results - depends on the structure that is unknown anyway
        // but we will just do breadth first without parallelization and call it a day

        // the method assumes that the provided path is existing and valid
        // which is not the best idea on larger scale codebase

        // the assumption is we won't run into the worst case scenario of millions of files
        // which would lead to much larger memory footprint that could exhaust the whole system memory etc.
        // basically we assume that the ram of the system will be enough which should cover the expected usage

        // we also assume that the snapshot will be enough
        // without write locking the actual source until the synchronization ends
        // which obviously may not be the case if someone is actively using it
        // but it will be overwritten on the next run anyway so
        // it is good enough at least for now

        var directories = new HashSet<string>();
        var files = new Dictionary<string, FileSnapshot>();

        var dirQueue = new Queue<string>();
        dirQueue.Enqueue(path);

        while (dirQueue.Count > 0)
        {
            var currentDir = dirQueue.Dequeue();
            if (currentDir != path)
            {
                var relativePath = Path.GetRelativePath(path, currentDir);
                directories.Add(relativePath);
            }

            string[] subDirs;
            string[] filePaths;

            subDirs = Directory.GetDirectories(currentDir);
            filePaths = Directory.GetFiles(currentDir);

            foreach (var subDir in subDirs)
            {
                dirQueue.Enqueue(subDir);
            }

            foreach (var filePath in filePaths)
            {
                // could also happen in parallel or at least concurrently after collecting all of the files
                // or even better enqueued to process as soon as they are available
                // this will take more time
                // but for this example should be fast enough
                using var fileStream = File.OpenRead(filePath);
                var checksum = await _checksumService.ChecksumAsync(fileStream);

                var fileInfo = new FileInfo(filePath);
                var size = fileInfo.Length;

                var relativePath = Path.GetRelativePath(path, filePath);
                files[relativePath] = new FileSnapshot
                {
                    Checksum = checksum,
                    Size = size
                };
            }
        }

        return new DirectorySnapshot
        {
            RootDirectoryPath = path,
            Directories = directories,
            Files = files,
        };
    }
}