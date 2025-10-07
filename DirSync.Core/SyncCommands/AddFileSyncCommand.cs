using DirSync.Core.SyncCommands.Enums;
using DirSync.Core.SyncCommands.Interfaces;

namespace DirSync.Core.SyncCommands;

public class AddFileSyncCommand(string sourcePath, string destinationPath, long size) : ISyncCommand
{
    private readonly string _sourcePath = sourcePath;
    private readonly string _destinationPath = destinationPath;
    private readonly long _size = size;
    public async Task ExecuteAsync()
    {
        // this is the only scenario where file size matters for the timeout
        // since actual data copy will occur here
        // there is a possibility this will be used to copy from/to another drive including one mounted from network

        // 5MB/s is reasonable speed to achieve
        // even with HDD copying multiple files (10 in this case)
        // with 2 seconds lower limit
        // this behavior is indeed kinda too hardcoded
        // because we are not guaranteed to always run with the assumed 10 commands at once
        // also for faster disk speeds the timeout doesn't make much sense too and degrades the behavior
        var bytesPerSecond = 5 * 1024 * 1024;
        var estimatedSeconds = Math.Max(2, _size / bytesPerSecond);
        var timeout = TimeSpan.FromSeconds(estimatedSeconds);

        var task = Task.Run(() =>
        {
            // we assume the file does not exist
            // it should be removed by the remove file command if everything went well
            // so we don't overwrite it to not mask any bugs that could occur
            File.Copy(_sourcePath, _destinationPath, overwrite: false);
        });

        var completedTask = await Task.WhenAny(task, Task.Delay(timeout));
        // if the timeout occured or file was not copied for any reason it will just throw and stop the proccess
        // it will retry it later anyway
        if (completedTask != task)
        {
            throw new TimeoutException($"{nameof(SyncCommandTypeEnum.AddFile)} {_sourcePath} {_destinationPath} timed out");
        }
        await completedTask;
    }

    public IEnumerable<string> DryRun()
    {
        return [
            nameof(SyncCommandTypeEnum.AddFile),
            _sourcePath,
            _destinationPath
        ];
    }
}