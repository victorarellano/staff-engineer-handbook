using Idempotency.Api.Repositories;

namespace Idempotency.Api.Repositories;
public interface IUnitOfWorkFactory
{
    Task<IUnitOfWork> CreateAsync();
}