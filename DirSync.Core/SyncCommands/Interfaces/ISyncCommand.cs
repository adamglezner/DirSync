namespace DirSync.Core.SyncCommands.Interfaces;

public interface ISyncCommand
{
    public Task ExecuteAsync();
    public IEnumerable<string> DryRun();
}