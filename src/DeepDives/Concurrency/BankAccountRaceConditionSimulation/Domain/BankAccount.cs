using Microsoft.Extensions.Logging;

namespace BankAccountRaceConditionSimulation.Domain;

public class BankAccount
{
    private readonly ILogger<BankAccount> _logger;

    public decimal Balance { get; private set; }
    private readonly object _balanceLock = new();

    public BankAccount(decimal initialBalance, ILogger<BankAccount> logger)
    {
        Balance = initialBalance;
        _logger = logger;
    }

    public async Task<bool> WithdrawUnsafeAsync(string operation, decimal amount, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Operation} reads balance {Balance}", operation, Balance);

        if (Balance < amount)
            return false;

        await Task.Delay(100, cancellationToken);

        Balance -= amount;

        return true;
    }

    public async Task<bool> WithdrawSafeAsync(string operation, decimal amount, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Operation} attempts withdrawal of {Amount}", operation, amount);

        await Task.Delay(100, cancellationToken);

        lock (_balanceLock)
        {
            _logger.LogInformation("{Operation} reads balance {Balance}", operation, Balance);

            if (Balance < amount)
            {
                _logger.LogInformation("{Operation} rejected. Balance {Balance}, requested {Amount}", operation, Balance, amount);

                return false;
            }

            _logger.LogInformation("{Operation} validates withdrawal of {Amount}", operation, amount);

            Balance -= amount;

            _logger.LogInformation("{Operation} writes new balance {Balance}", operation, Balance);

            return true;
        }
    }
}
