using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Nest;

namespace Stock.Ingest
{
    public class StockIngestWorker : BackgroundService
    {
        private readonly ILogger<StockIngestWorker> _logger;
        private readonly StockDataReader _stockDataReader;
        private readonly IElasticClient _elasticClient;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public StockIngestWorker(ILogger<StockIngestWorker> logger, StockDataReader stockDataReader,
            IElasticClient elasticClient, IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _stockDataReader = stockDataReader;
            _elasticClient = elasticClient;
            _applicationLifetime = applicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            var bulkAll = _elasticClient.BulkAll(_stockDataReader.StockData(), b => b
                .Index("stock-demo-v1")
                .BackOffRetries(2)
                .BackOffTime("30s")
                .MaxDegreeOfParallelism(4)
                .Size(1000));

            bulkAll.Wait(TimeSpan.FromMinutes(30), _ => _logger.LogInformation("Data Indexed"));
            await _elasticClient.Indices.PutAliasAsync("stock-demo-v1", "stock-demo", ct: stoppingToken);

            _applicationLifetime.StopApplication();
        }
    }
}
