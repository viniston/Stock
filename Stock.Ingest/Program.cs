using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nest;
using Stock.Domain;

namespace Stock.Ingest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    // Ingest
                    services.AddHostedService<StockIngestWorker>();

                    // Re-indexing
                    //services.AddHostedService<ReindexWorker>();

                    services.AddSingleton<IElasticClient>(serviceProvider =>
                    {
                        var config = serviceProvider.GetRequiredService<IConfiguration>();
                        var settings = new ConnectionSettings(config["cloudId"],
                                new BasicAuthenticationCredentials("elastic", config["password"]))
                            .DefaultIndex("an-example-index")
                            .DefaultMappingFor<StockData>(i => i
                                .IndexName("stock-demo-v1"));

                        return new ElasticClient(settings);

                    });
                    services.AddSingleton<StockDataReader>();
                });
    }
}
