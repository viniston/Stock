using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Nest;
using Stock.Domain;

namespace Stock.Ingest
{
    public class ReindexWorker : BackgroundService
    {
        private readonly ILogger<StockIngestWorker> _logger;
        private readonly IElasticClient _elasticClient;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public ReindexWorker(ILogger<StockIngestWorker> logger, IElasticClient elasticClient,
            IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _elasticClient = elasticClient;
            _applicationLifetime = applicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            const string newIndexName = "stock-demo-v2";
            const string oldIndexName = "stock-demo-v1";
            var response = await _elasticClient.Indices.ExistsAsync(newIndexName, ct: stoppingToken);
            if (response.Exists)
            {
                await _elasticClient.Indices.DeleteAsync(newIndexName, ct: stoppingToken);
            }

            var newIndexResponse =
                await _elasticClient.Indices.CreateAsync(newIndexName, i => i
                    .Map(m => m
                        .AutoMap<StockData>()
                        .Properties<StockData>(p => p
                            .Keyword(k => k.Name(f => f.Symbol)))), stoppingToken);
            if (newIndexResponse.IsValid)
            {
                _logger.LogInformation("Create new index");

                var reindex = await _elasticClient.ReindexOnServerAsync(r => r
                    .Source(s => s.Index(oldIndexName))
                    .Destination(d => d.Index(newIndexName))
                    .WaitForCompletion(false), stoppingToken);

                var taskId = reindex.Task;
                var taskResponse = await _elasticClient.Tasks.GetTaskAsync(taskId, ct: stoppingToken);

                while (!taskResponse.Completed)
                {
                    _logger.LogInformation("Waiting for 5 seconds");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    taskResponse = await _elasticClient.Tasks.GetTaskAsync(taskId, ct: stoppingToken);
                }

                _logger.LogInformation("Reindex completed");

                await _elasticClient.Indices.BulkAliasAsync(aliases => aliases
                    .Remove(a => a.Alias("stock-demo").Index("*"))
                    .Add(a => a.Alias("stock-demo").Index(newIndexName)), stoppingToken);

                _logger.LogInformation("Alias updated");
            }

            _applicationLifetime.StopApplication();
        }
    }
}
