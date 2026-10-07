namespace ButcherShopSimulation.Configuration
{
    public sealed class ButcherShopSettings
    {
        public const string SectionName = "ButcherShopSettings";

        public int ButchersCount { get; set; }
        public int MaxTicketsPerDay { get; set; }
        public string OpeningHour { get; set; } = "09:00";
        public string ClosingHour { get; set; } = "18:00";
        public string LunchStart { get; set; } = "12:00";
        public string LunchEnd { get; set; } = "14:00";
        public int ServiceTimeMinMinutes { get; set; } = 1;
        public int ServiceTimeMaxMinutes { get; set; } = 3;
        public int ArrivalTimeMinMs { get; set; } = 500;
        public int ArrivalTimeMaxMs { get; set; } = 1500;
    }
}
