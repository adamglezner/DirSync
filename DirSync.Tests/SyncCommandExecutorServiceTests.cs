using Moq;
using DirSync.Core.SyncCommands.Services;
using Microsoft.Extensions.Logging;
using DirSync.Core.SyncCommands.Interfaces;

namespace DirSync.Tests;

public class SyncCommandExecutorServiceTests
{
    private SyncCommandExecutorService _syncCommandExecutorService;

    [SetUp]
    public void SetUp()
    {
        _syncCommandExecutorService = new SyncCommandExecutorService(new Mock<ILogger<SyncCommandExecutorService>>().Object);
    }

    [Test]
    public async Task ExecuteAsync_CallsExecuteOnAllCommands()
    {
        var command1 = new Mock<ISyncCommand>();
        var command2 = new Mock<ISyncCommand>();

        command1.Setup(c => c.ExecuteAsync()).Returns(Task.CompletedTask);
        command2.Setup(c => c.ExecuteAsync()).Returns(Task.CompletedTask);

        command1.Setup(c => c.DryRun()).Returns(["cmd1"]);
        command2.Setup(c => c.DryRun()).Returns(["cmd2"]);

        var commands = new List<ISyncCommand> { command1.Object, command2.Object };

        await _syncCommandExecutorService.ExecuteAsync(commands, batchSize: 1);

        command1.Verify(c => c.ExecuteAsync(), Times.Once);
        command2.Verify(c => c.ExecuteAsync(), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_UsesCorrectBatchSize()
    {
        var executedCommands = new List<int>();
        var commands = Enumerable.Range(1, 10).Select(i =>
        {
            var cmd = new Mock<ISyncCommand>();
            cmd.Setup(c => c.ExecuteAsync()).Returns(() =>
            {
                executedCommands.Add(i);
                return Task.CompletedTask;
            });
            cmd.Setup(c => c.DryRun()).Returns([$"cmd{i}"]);
            return cmd.Object;
        }).ToList();

        await _syncCommandExecutorService.ExecuteAsync(commands, batchSize: 2);

        Assert.That(executedCommands.Count, Is.EqualTo(10));
    }

    [Test]
    public void ExecuteAsync_HandlesEmptyCommandList()
    {
        var commands = new List<ISyncCommand>();

        Assert.DoesNotThrowAsync(async () => await _syncCommandExecutorService.ExecuteAsync(commands, batchSize: 3));
    }
}