using System;
using System.Collections.Generic;

namespace Stock.Domain
{
    public class StockData
    {
        private static readonly Dictionary<string, string> CompanyLookup = new()
        {
            {"AAL", "American Airlines Group Inc"},
            {"MSFT", "Microsoft Corporation"},
            {"AME", "AMETEK, Inc."},
            {"M", "Macy's Inc"}
        };

        public StockData(string dataLine)
        {
            var columns = dataLine.Split(',', StringSplitOptions.TrimEntries);

            Symbol = columns[6];

            if (DateTime.TryParse(columns[0], out var date))
            {
                Date = date;
            }

            if (double.TryParse(columns[1], out var open))
            {
                Open = open;
            }

            if (double.TryParse(columns[2], out var high))
            {
                High = high;
            }

            if (double.TryParse(columns[3], out var low))
            {
                Low = low;
            }

            if (double.TryParse(columns[4], out var close))
            {
                Close = close;
            }

            if (uint.TryParse(columns[5], out var volume))
            {
                Volume = volume;
            }

            if (CompanyLookup.TryGetValue(columns[6], out var name))
            {
                Name = name;
            }

        }

        public DateTime Date { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public string Symbol { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public uint Volume { get; set; }
        public string Name { get; set; }

    }
}
