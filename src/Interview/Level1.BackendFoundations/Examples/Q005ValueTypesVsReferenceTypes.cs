namespace Level1.BackendFoundations.Examples;

public static class Q005ValueTypesVsReferenceTypes
{
    public const string Number = "005";
    public const string Title = "Value Types vs Reference Types";

    public static void Run()
    {
        Console.WriteLine("Value type example:");
        Console.WriteLine();

        var originalNumber = 10;
        var copiedNumber = originalNumber;
        copiedNumber = 20;

        Console.WriteLine($"Original number: {originalNumber}");
        Console.WriteLine($"Copied number:   {copiedNumber}");

        Console.WriteLine();
        Console.WriteLine("Reference type example:");
        Console.WriteLine();

        var originalPerson = new Person("Victor");
        var referencedPerson = originalPerson;
        referencedPerson.Name = "John";

        Console.WriteLine($"Original reference: {originalPerson.Name}");
        Console.WriteLine($"Copied reference:   {referencedPerson.Name}");
    }

    private sealed class Person(string name)
    {
        public string Name { get; set; } = name;
    }
}
