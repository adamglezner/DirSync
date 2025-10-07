using DirSync.Core.Checksum.Interfaces;
using DirSync.Core.Checksum.Services;
using DirSync.Core.DirectoryScanner.Services;
using DirSync.Core.SyncCommands.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DirSync.Core.SyncCommands.Enums;
using Serilog;

if (args.Length < 2 || args.Length > 4)
{
    Console.WriteLine("Usage: <sourceDirectoryPath> <replicaDirectoryPath> [<intervalInSeconds>] [<logFilePath>]");
    return;
}

var sourceDirPath = args[0];
var replicaDirPath = args[1];
var intervalInSecondsString = args.Length > 2 ? args[2] : "";
var logFilePath = args.Length > 3 ? args[3] : "";

if (!Directory.Exists(sourceDirPath))
{
    Console.WriteLine($"Provided source directory does not exist: {sourceDirPath}");
    return;
}

if (!Directory.Exists(replicaDirPath))
{
    Console.WriteLine($"Provided replica directory does not exist: {replicaDirPath}");
    return;
}

var intervalInSeconds = 60;
if (intervalInSecondsString != ""
&& !int.TryParse(intervalInSecondsString, out intervalInSeconds)
|| intervalInSeconds <= 0)
{
    Console.WriteLine($"Provided incorrect interval: {intervalInSecondsString}");
    return;
}

if (logFilePath != "" && !Directory.Exists(Path.GetDirectoryName(logFilePath)))
{
    Console.WriteLine($"Log directory does not exist: {Path.GetDirectoryName(logFilePath)}");
    return;
}

if (logFilePath != ""
&& Path.GetFileNameWithoutExtension(logFilePath) == string.Empty
&& Path.GetExtension(logFilePath) == string.Empty)
{
    Console.WriteLine($"Provided log path to a directory not to a file: {logFilePath}");
    return;
}

if (logFilePath == "")
{
    logFilePath = $"./{DateTime.Now:yyyyMMddHHmmss}.log";
}


var builder = Host.CreateApplicationBuilder();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(
        path: logFilePath,
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        retainedFileCountLimit: 7)
    .CreateLogger();
builder.Services.AddSerilog();

builder.Services.AddSingleton<IChecksumService, Sha256ChecksumService>();
builder.Services.AddSingleton<DirectoryScannerService>();
builder.Services.AddSingleton<SyncCommandGeneratorService>();
builder.Services.AddSingleton<SyncCommandExecutorService>();
using var host = builder.Build();

var directoryScanner = host.Services.GetRequiredService<DirectoryScannerService>();
var syncCommandGenerator = host.Services.GetRequiredService<SyncCommandGeneratorService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// directory commands don't run the best in parallel/concurrently due to implementation
// so let's not pretend like it works without issues and run it serially for now (batchSize: 1)
var executionConfig = new List<(SyncCommandTypeEnum Type, int BatchSize)>
{
    (SyncCommandTypeEnum.AddDirectory, 1),
    (SyncCommandTypeEnum.RemoveFile, 10),
    (SyncCommandTypeEnum.AddFile, 10),
    (SyncCommandTypeEnum.RemoveDirectory, 1)
};

var syncCommandExecutor = host.Services.GetRequiredService<SyncCommandExecutorService>();

// there's no need to bring an actual scheduler there yet
// so let's just go for good old main loop
// which for this case will be good enough
while (true)
{
    // cancellation tokens in this specific case are irrelevant
    // so they were skipped
    try
    {
        logger.LogInformation("Synchronization start");
        var sourceScanTask = directoryScanner.ScanAsync(sourceDirPath);
        var replicaScanTask = directoryScanner.ScanAsync(replicaDirPath);

        logger.LogInformation($"Scanning directories source: {sourceDirPath} replica: {replicaDirPath}");
        await Task.WhenAll(sourceScanTask, replicaScanTask);
        logger.LogInformation($"Directories source: {sourceDirPath} replica: {replicaDirPath} scanned");

        var commandsDictionary = syncCommandGenerator.GenerateSyncCommands(sourceScanTask.Result, replicaScanTask.Result);
        foreach (var (type, batchSize) in executionConfig)
        {
            if (!commandsDictionary.TryGetValue(type, out var commands))
            {
                continue;
            }
            logger.LogInformation($"Executing commands of type: {type}");
            await syncCommandExecutor.ExecuteAsync(commands, batchSize);
        }
        logger.LogInformation("Synchronization end");
    }
    catch (Exception ex)
    {
        // we don't really need granular exception handling for this case
        // and any specific exception recoveries
        // we just want it to not crash and retry later
        logger.LogError($"Synchronization failed: {ex}");
    }
    // scheduling this to run again on exact times like
    // every 60 seconds no matter what
    // and then checking if this "job" is already running
    // doesn't seem like the best idea
    // so we just schedule it when the whole process ends
    // no matter if it ended correctly or not
    // and without any exponential backoffs etc.
    // with the interval as delay
    // I think the goal was not to use or simulate job scheduler that behaves like cron
    // and this scheduling meets the expectations
    await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds));
}