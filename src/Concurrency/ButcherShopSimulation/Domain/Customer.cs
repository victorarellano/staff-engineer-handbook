namespace ButcherShopSimulation.Domain
{
    public class Customer
    {
        public int TicketNumber { get; }
        public DateTime ArrivalTime { get; }

        public Customer(int ticketNumber)
        {
            TicketNumber = ticketNumber;
            ArrivalTime = DateTime.Now;
        }
    }
}