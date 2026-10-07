using Microsoft.Extensions.Logging;

namespace ButcherShopSimulation.Domain
{
    public class Statistics
    {
        private readonly List<TimeSpan> _waitTimes = new();
        private readonly List<TimeSpan> _serviceTimes = new();
        private int _servedCustomers = 0;
        private int _rejectedCustomers = 0;

        public void RegisterWaitTime(TimeSpan waitTime)
        {
            lock (_waitTimes) _waitTimes.Add(waitTime);
        }

        public void RegisterService(TimeSpan serviceTime, TimeSpan totalTime)
        {
            lock (_serviceTimes) _serviceTimes.Add(serviceTime);
            Interlocked.Increment(ref _servedCustomers);
        }

        public void RegisterRejected()
        {
            Interlocked.Increment(ref _rejectedCustomers);
        }

        public void PrintReport(ILogger logger)
        {
            var avgWait = _waitTimes.Count > 0 ? _waitTimes.Average(w => w.TotalMinutes) : 0;
            var maxWait = _waitTimes.Count > 0 ? _waitTimes.Max(w => w.TotalMinutes) : 0;
            var avgService = _serviceTimes.Count > 0 ? _serviceTimes.Average(s => s.TotalMinutes) : 0;

            logger.LogInformation("=== Daily Statistics Report ===");
            logger.LogInformation("Total served customers: {Count}", _servedCustomers);
            logger.LogInformation("Total rejected customers: {Count}", _rejectedCustomers);
            logger.LogInformation("Average wait time: {Avg:F2} minutes", avgWait);
            logger.LogInformation("Max wait time: {Max:F2} minutes", maxWait);
            logger.LogInformation("Average service time: {Avg:F2} minutes", avgService);
        }
    }
}
