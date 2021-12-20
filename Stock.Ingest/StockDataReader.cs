using Microsoft.Extensions.Logging;
using Stock.Domain;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stock.Ingest
{
    public class StockDataReader
    {
        private readonly ILogger<StockDataReader> _logger;

        public StockDataReader(ILogger<StockDataReader> logger)
        {
            _logger = logger;
        }

        public List<StockData> StockData()
        {
            _logger.LogInformation("Reading the stock information");
            var fileLocation = $@"{Path.Combine(Directory.GetCurrentDirectory(), "all_stocks_5yr.csv")}";
            var lines = File.ReadLines(fileLocation);
            _logger.LogInformation("Sending the stock information");
            return lines.Where(line => !line.Contains("volume")).Select(line => new StockData(line)).ToList();
        }
    }
}
