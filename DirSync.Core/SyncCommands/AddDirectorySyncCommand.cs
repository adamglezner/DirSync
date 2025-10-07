using DirSync.Core.SyncCommands.Enums;
using DirSync.Core.SyncCommands.Interfaces;

namespace DirSync.Core.SyncCommands;
public class AddDirectorySyncCommand(string path) : ISyncCommand
{
    private readonly string _path = path;

    public async Task ExecuteAsync()
    {
        // timeout is just static 2 seconds
        // the timeout with current assumptions of basically executing commands serially is very much sane
        // but obviously executing commands serially is bad itself
        // if you can't create a directory in 2 seconds
        // (assumed here is also recursively)
        // then there are bigger issues that should concern you
        // there is a possibility this will be used to create directory on another drive including one mounted from network

        var task = Task.Run(() => Directory.CreateDirectory(_path));

        var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));
        // if the timeout occured or directory was not created for any reason it will just throw and stop the proccess
        // it will retry it later anyway
        if (completedTask != task)
        {
            throw new TimeoutException($"{nameof(SyncCommandTypeEnum.AddDirectory)} {_path} timed out");
        }
        await completedTask;
    }

    public IEnumerable<string> DryRun()
    {
        return [
            nameof(SyncCommandTypeEnum.AddDirectory),
            _path
        ];
    }
}