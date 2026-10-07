namespace ProducerConsumerSimulation.Logging;

public static class ConsoleColors
{
    public const string Reset = "\u001b[0m";
    public const string Request = "\u001b[32m";   // Green
    public const string Acquired = "\u001b[36m";  // Cyan
    public const string Completed = "\u001b[33m"; // Yellow
    public const string Released = "\u001b[34m"; // Blue
    public const string Error = "\u001b[31m"; // Red
}