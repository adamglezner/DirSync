using DirSync.Core.DirectoryScanner.Models;
using DirSync.Core.SyncCommands.Interfaces;
using DirSync.Core.SyncCommands.Enums;

namespace DirSync.Core.SyncCommands.Services;

public class SyncCommandGeneratorService
{
    public Dictionary<SyncCommandTypeEnum, IEnumerable<ISyncCommand>> GenerateSyncCommands(DirectorySnapshot source, DirectorySnapshot replica)
    {
        // if used directly this forces the user of this code
        // to make sure to execute all of the commands in the correct order
        // the order of commands doesn't exactly seem like the Generator's job
        // in this small project it doesn't seem too bad
        // but shouldn't be common otherwise

        // it does actually return the correct order
        // but this behavior shouldn't be relied upon
        // anyone touching this code f.e. adding new command
        // is not forced to add it in correct order

        // methods in this class aren't a work of art
        // some refactor should be considered
        return new Dictionary<SyncCommandTypeEnum, IEnumerable<ISyncCommand>>
        {
            { SyncCommandTypeEnum.AddDirectory, GenerateAddDirectorySyncCommands(source, replica) },
            { SyncCommandTypeEnum.RemoveFile, GenerateRemoveFileSyncCommands(source, replica) },
            { SyncCommandTypeEnum.AddFile, GenerateAddFileSyncCommands(source, replica) },
            { SyncCommandTypeEnum.RemoveDirectory, GenerateRemoveDirectorySyncCommands(source, replica) }
        };
    }
    private IEnumerable<AddDirectorySyncCommand> GenerateAddDirectorySyncCommands(DirectorySnapshot source, DirectorySnapshot replica)
    {
        // could just generate the minimal commands to recreate the directory structure recursively
        // but it currently doesn't
        var toAdd = source.Directories
        .Except(replica.Directories)
        .OrderBy(d => d.Length);

        foreach (var dir in toAdd)
        {
            var fullReplicaPath = Path.Combine(replica.RootDirectoryPath, dir);
            yield return new AddDirectorySyncCommand(fullReplicaPath);
        }
        
    }
    private IEnumerable<RemoveDirectorySyncCommand> GenerateRemoveDirectorySyncCommands(DirectorySnapshot source, DirectorySnapshot replica)
    {
        // could just generate the minimal commands to remove the directory structure recursively
        // but it currently doesn't
        var toRemove = replica.Directories
        .Except(source.Directories)
        .OrderByDescending(d => d.Length);

        foreach (var dir in toRemove)
        {
            var fullReplicaPath = Path.Combine(replica.RootDirectoryPath, dir);
            yield return new RemoveDirectorySyncCommand(fullReplicaPath);
        }
    }

    private IEnumerable<AddFileSyncCommand> GenerateAddFileSyncCommands(DirectorySnapshot source, DirectorySnapshot replica)
    {
        foreach (var kvp in source.Files)
        {
            var sourceFilePath = kvp.Key;
            var sourceFile = kvp.Value;

            var fileExistsInBoth = replica.Files.TryGetValue(sourceFilePath, out var replicaFile);
            var checksumsMatch = replicaFile != null && sourceFile.Checksum == replicaFile.Checksum;
            if (!fileExistsInBoth || !checksumsMatch)
            {
                var fullSourcePath = Path.Combine(source.RootDirectoryPath, sourceFilePath);
                var fullReplicaPath = Path.Combine(replica.RootDirectoryPath, sourceFilePath);
                yield return new AddFileSyncCommand(fullSourcePath, fullReplicaPath, sourceFile.Size);
            }
        }
    }

    private IEnumerable<RemoveFileSyncCommand> GenerateRemoveFileSyncCommands(DirectorySnapshot source, DirectorySnapshot replica)
    {
        // this method actually does two things at once
        // should be separated
        var nonExistingInSource = replica.Files.Keys
        .Except(source.Files.Keys);

        foreach (var filePath in nonExistingInSource)
        {
            var fullReplicaPath = Path.Combine(replica.RootDirectoryPath, filePath);
            yield return new RemoveFileSyncCommand(fullReplicaPath);
        }

        foreach (var kvp in source.Files)
        {
            var sourceFilePath = kvp.Key;
            var sourceFile = kvp.Value;

            var fileExistsInBoth = replica.Files.TryGetValue(sourceFilePath, out var replicaFile);
            var checksumsMatch = replicaFile != null && sourceFile.Checksum == replicaFile.Checksum;
            if (fileExistsInBoth && !checksumsMatch)
            {
                var fullReplicaPath = Path.Combine(replica.RootDirectoryPath, sourceFilePath);
                yield return new RemoveFileSyncCommand(fullReplicaPath);
            }
        }
    }
}