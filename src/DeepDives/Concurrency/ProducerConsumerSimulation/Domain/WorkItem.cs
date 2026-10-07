using Microsoft.Extensions.Logging;

namespace ProducerConsumerSimulation.Domain
{
    public sealed class WorkItem
    {
        public int Id { get; }

        public DateTime CreatedAt { get; }

        public WorkItem(int id)
        {
            Id = id;
            CreatedAt = DateTime.Now;
        }
    }
}
