namespace Level1.BackendFoundations.Examples;

public static class Q002GarbageCollection
{
    public const string Number = "002";
    public const string Title = "Garbage Collection";

    public static void Run()
    {
        var temporaryObject = new SampleObject("Temporary object");

        Console.WriteLine(
            $"Initial generation: {GC.GetGeneration(temporaryObject)}");

        GC.Collect();
        GC.WaitForPendingFinalizers();

        Console.WriteLine(
            $"Generation after first collection: " +
            $"{GC.GetGeneration(temporaryObject)}");

        GC.Collect();
        GC.WaitForPendingFinalizers();

        Console.WriteLine(
            $"Generation after second collection: " +
            $"{GC.GetGeneration(temporaryObject)}");

        Console.WriteLine();
        Console.WriteLine(
            "The object remains alive because the local variable still " +
            "references it.");

        Console.WriteLine();
        Console.WriteLine(
            "GC.Collect() is used here only to demonstrate generations. " +
            "It should normally not be called by application code.");
    }

    private sealed record SampleObject(string Name);
}