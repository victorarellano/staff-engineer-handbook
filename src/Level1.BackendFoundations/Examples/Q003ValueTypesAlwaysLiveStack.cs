namespace Level1.BackendFoundations.Examples;

public static class Q003ValueTypesAlwaysLiveStack
{
    public const string Number = "003";

    public const string Title = "Do Value Types Always Live on the Stack?";

    public static void Run()
    {
        Console.WriteLine("=== Local Value Type ===");
        Console.WriteLine();

        var gender = new Gender(GenderType.Male);

        Console.WriteLine($"Gender: {gender}");

        Console.WriteLine();
        Console.WriteLine("=== Value Type Inside a Reference Type ===");
        Console.WriteLine();

        var person = new Person(gender);

        Console.WriteLine($"Person Gender: {person.Gender}");

        Console.WriteLine();
        Console.WriteLine("Explanation:");
        Console.WriteLine("- 'gender' is a value type.");
        Console.WriteLine("- 'person' is a reference type.");
        Console.WriteLine("- The Gender field is stored inside the Person object.");
        Console.WriteLine("- Therefore, not every value type lives on the stack.");
    }

    public enum GenderType
    {
        Female,
        Male,
        NonBinary,
        Unspecified
    }

    public readonly record struct Gender(GenderType Value);

    public sealed class Person
    {
        public Gender Gender { get; }

        public Person(Gender gender)
        {
            Gender = gender;
        }
    }
}