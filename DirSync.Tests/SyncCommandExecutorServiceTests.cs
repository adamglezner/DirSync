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
    public async Task ExecuteAsync_ExecutesAllCommandsOnce()
    {
        var command1 = new Mock<ISyncCommand>();
        var command2 = new Mock<ISyncCommand>();

        command1.Setup(c => c.ExecuteAsync()).Returns(Task.CompletedTask);
        command2.Setup(c => c.ExecuteAsync()).Returns(Task.CompletedTask);

        command1.Setup(c => c.DryRun()).Returns(["cmd1"]);
        command2.Setup(c => c.DryRun()).Returns(["cmd2"]);

        var commands = new List<ISyncCommand> { command1.Object, command2.Object };

        await _syncCommandExecutorService.ExecuteAsync(commands, batchSize: 10);

        command1.Verify(c => c.ExecuteAsync(), Times.Once);
        command2.Verify(c => c.ExecuteAsync(), Times.Once);
    }

    [Test]
    public void ExecuteAsync_HandlesEmptyCommandList()
    {
        var commands = new List<ISyncCommand>();

        Assert.DoesNotThrowAsync(async () => await _syncCommandExecutorService.ExecuteAsync(commands, 10));
    }
}