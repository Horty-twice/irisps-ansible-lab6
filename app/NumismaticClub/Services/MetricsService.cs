using System.Diagnostics.Metrics;

namespace NumismaticClub.Services
{
    public class MetricsService
    {
        private readonly Counter<int> _coinsAddedCounter;

        public MetricsService(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("NumismaticClub");
            _coinsAddedCounter = meter.CreateCounter<int>(
                "coins_added_total",
                description: "Total number of coins added to the collection"
            );
        }

        public void RecordCoinAdded()
        {
            _coinsAddedCounter.Add(1);
        }
    }
}