using DirSync.Core.SyncCommands.Interfaces;
using Microsoft.Extensions.Logging;

namespace DirSync.Core.SyncCommands.Services;

public class SyncCommandExecutorService(ILogger<SyncCommandExecutorService> logger)
{
    private readonly ILogger<SyncCommandExecutorService> _logger = logger;
    public async Task ExecuteAsync(IEnumerable<ISyncCommand> commands, int batchSize)
    {
        // batchSize is configurable
        // increasing it depending on the actual disk type (SSD, HDD)
        // may either speed up the performance in case of SSD
        // or lower it in case of HDD
        // the ideal case for HDD could be actually 1 just because of the physical reading - reading one big file (not really tested just a hunch)
        // 10 seems like an OK starting point even for bad case scenarios like really slow HDD
        // while still parallelizing work in case of faster disk speeds of any SSD that deals better with accessing many files at once
        // setting it to 1 also enables the commands to be run serially
        // there are no checks if the provided value is sane

        // in case of many small files which is common scenario
        // there is really no upper limit to how many commands could be generated
        // which could go to even millions if you just have a lot of files
        // so we are getting the actual command objects lazily in batches
        // to avoid even larger memory footprint than it already has since we skipped this part already in the DirectoryScanner
        foreach (var currentBatch in Batch(commands, batchSize))
        {
            await Task.WhenAll(currentBatch.Select(c =>
            {
                _logger.LogInformation($"Executing command: {string.Join(" ", c.DryRun())}");
                return c.ExecuteAsync();
            }));
        }
    }

    private IEnumerable<List<ISyncCommand>> Batch(IEnumerable<ISyncCommand> commands, int batchSize)
    {
        using var enumerator = commands.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var batch = new List<ISyncCommand>(batchSize) { enumerator.Current };

            for (int i = 1; i < batchSize && enumerator.MoveNext(); i++)
            {
                batch.Add(enumerator.Current);
            }

            yield return batch;
        }
    }
}