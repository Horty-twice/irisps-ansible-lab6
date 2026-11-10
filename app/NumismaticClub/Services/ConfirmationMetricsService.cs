using System.Diagnostics.Metrics;

namespace NumismaticClub.Services
{
    public class ConfirmationMetricsService
    {
        private readonly Counter<int> _confirmationRequestsCounter;
        private readonly Counter<int> _confirmationSuccessCounter;
        private readonly Histogram<double> _confirmationDelayHistogram;

        public ConfirmationMetricsService(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("NumismaticClub.Confirmations");

            _confirmationRequestsCounter = meter.CreateCounter<int>(
                "confirmation_requests_total",
                description: "Total number of confirmation requests sent");

            _confirmationSuccessCounter = meter.CreateCounter<int>(
                "confirmation_success_total",
                description: "Total number of successfully confirmed coins");

            _confirmationDelayHistogram = meter.CreateHistogram<double>(
                "confirmation_delay_seconds",
                unit: "s",
                description: "Time between coin creation and confirmation");
        }

        public void RecordConfirmationRequest()
        {
            _confirmationRequestsCounter.Add(1);
        }

        public void RecordConfirmationSuccess(double delaySeconds)
        {
            _confirmationSuccessCounter.Add(1);
            _confirmationDelayHistogram.Record(delaySeconds);
        }
    }
}