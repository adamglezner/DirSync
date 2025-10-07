using DirSync.Core.SyncCommands.Enums;
using DirSync.Core.SyncCommands.Interfaces;

namespace DirSync.Core.SyncCommands;

public class RemoveDirectorySyncCommand(string path) : ISyncCommand
{
    private readonly string _path = path;
    public async Task ExecuteAsync()
    {
        // timeout is just static 2 seconds
        // the timeout with current assumptions of basically executing commands serially is very much sane
        // but obviously executing commands serially is bad itself
        // if you can't remove an empty directory in 2 seconds
        // (assumed here is either fully empty or just with empty directories inside)
        // then there are bigger issues that should concern you
        // there is a possibility this will be used to remove directory from another drive including one mounted from network

        var task = Task.Run(() =>
        {
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, recursive: true);
            }
        });

        var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));
        // if the timeout occured or directory was not removed for any reason it will just throw and stop the proccess
        // it will retry it later anyway
        if (completedTask != task)
        {
            throw new TimeoutException($"{nameof(SyncCommandTypeEnum.RemoveDirectory)} {_path} timed out");
        }
        await completedTask;
    }

    public IEnumerable<string> DryRun()
    {
        return [
            nameof(SyncCommandTypeEnum.RemoveDirectory),
            _path,
        ];
    }
}