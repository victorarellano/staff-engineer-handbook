namespace BankTransferDeadlockSimulation.Domain;

public sealed class BankAccount
{
    public int Id { get; }
    public string Name { get; }
    public decimal Balance { get; private set; }

    internal object SyncRoot { get; } = new();

    public BankAccount(int id, string name, decimal initialBalance)
    {
        Id = id;
        Name = name;
        Balance = initialBalance;
    }

    internal void Withdraw(decimal amount)
    {
        Balance -= amount;
    }

    internal void Deposit(decimal amount)
    {
        Balance += amount;
    }
}