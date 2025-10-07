using DirSync.Core.SyncCommands.Enums;
using DirSync.Core.SyncCommands.Interfaces;

namespace DirSync.Core.SyncCommands;

public class RemoveFileSyncCommand(string path) : ISyncCommand
{
    private readonly string _path = path;
    public async Task ExecuteAsync()
    {
        // timeout is just static 2 seconds
        // this timeout could be too hardcoded
        // because we assume the number of commands executed at once is sane like 10
        // and the directory doesn't contain insane number of files which may slow down the removal of reference to it
        // if you can't remove a file in 2 seconds
        // which OS/FileSystem internally should just remove the reference to it
        // then this action was either deffered/blocked or there are bigger issues that should concern you
        // there is a possibility this will be used to remove file from another drive including one mounted from network
        var task = Task.Run(() =>
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        });

        var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));
        // if the timeout occured or directory was not removed for any reason it will just throw and stop the proccess
        // it will retry it later anyway
        if (completedTask != task)
        {
            throw new TimeoutException($"{nameof(SyncCommandTypeEnum.RemoveFile)} {_path} timed out");
        }
        await completedTask;
    }

    public IEnumerable<string> DryRun()
    {
        return [
            nameof(SyncCommandTypeEnum.RemoveFile),
            _path,
        ];
    }
}