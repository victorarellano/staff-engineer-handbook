namespace RateLimitedServiceSimulation.Configuration;

public sealed class RateLimitedServiceSettings
{
    public const string SectionName = "RateLimitedServiceSettings";
    public int RequestCount { get; set; }
    public int ExternalLimitPerSecond { get; set; }
    public int ProcessingDelayMilliseconds { get; set; } 
    public Decimal SafetyMargin    { get; set; }
    public int MaxAttempts { get; set; }     
}