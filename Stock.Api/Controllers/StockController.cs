using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using System.Threading.Tasks;
using Stock.Domain;

namespace Stock.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StockController : ControllerBase
    {

        private readonly ILogger<StockController> _logger;
        private readonly IElasticClient _elasticClient;

        public StockController(ILogger<StockController> logger, IElasticClient elasticClient)
        {
            _logger = logger;
            _elasticClient = elasticClient;
        }

        [HttpGet("symbol")]
        public async Task<IActionResult> Get()
        {
            var response = await _elasticClient.SearchAsync<StockData>(s =>
                s.Aggregations(a =>
                    a.Terms("symbols", t => t.Field(f => f.Symbol).Size(500))));

            var request = new SearchRequest<StockData>
            {
                Aggregations = new TermsAggregation("symbols")
                {
                    Field = Infer.Field<StockData>(f => f.Symbol),
                    Size = 500
                }
            };

            response = await _elasticClient.SearchAsync<StockData>(request);

            if (!response.IsValid)
            {
                return NotFound();
            }

            var symbols = response.Aggregations.Terms("symbols").Buckets.Select(b => b.Key).ToList();

            if (symbols.Any())
            {
                return Content(string.Join(",\r\n", symbols));
            }

            return NotFound();

        }

        [HttpGet("symbol/{symbol}")]
        public async Task<IActionResult> GetSymbol(string symbol)
        {
            var response = await _elasticClient.SearchAsync<StockData>(s => s.Query(q => q.Bool(b => b
                    .Filter(f => f.Term(t => t.Field(fld => fld.Symbol).Value(symbol))))).Size(25)
                .Sort(srt => srt.Descending(f => f.Date)));

            if (!response.IsValid)
            {
                return NotFound();
            }

            var sb = new StringBuilder();
            foreach (var doc in response.Documents)
            {
                sb.AppendLine($"{doc.Date,-12:d}{doc.Low,8:F2}{doc.High,8:F2}");
            }

            return Content(sb.ToString());

        }

        [HttpGet("volume/{symbol}")]
        public async Task<IActionResult> GetVolume(string symbol)
        {
            var response = await _elasticClient.SearchAsync<StockData>(s => s.Query(q => q.Bool(b => b
                    .Filter(f => f.Term(t => t.Field(fld => fld.Symbol).Value(symbol))))).Size(25)
                .Sort(srt => srt.Descending(f => f.Date)));

            if (!response.IsValid)
            {
                return NotFound();
            }

            var sb = new StringBuilder();
            foreach (var doc in response.Documents)
            {
                sb.AppendLine($"{doc.Date,-12:d}{doc.Volume}");
            }

            return Content(sb.ToString());

        }
    }
}
