# Idempotency Labs

These labs progressively explore the guarantees required to make backend
operations idempotent.

They accompany:

`docs/deep-dives/integration/idempotency.md`

## 01 - Naive

Example: [`Idempotency.Naive.Api`](./01-Naive/Idempotency.Naive.Api/) ·
[`Integration Tests`](./01-Naive/Idempotency.Naive.IntegrationTests/)

Demonstrates the initial problem.

Two HTTP requests containing the same payload are treated as two
independent operations and therefore create two different expenses.

The server has no information that allows it to distinguish a retry from
a new business operation.

## 02 - In-Memory Idempotency

Example:
[`Idempotency.InMemory.Api`](./02-InMemory/Idempotency.InMemory.Api/) ·
[`Integration Tests`](./02-InMemory/Idempotency.InMemory.IntegrationTests/)

Introduces a client-generated `Idempotency-Key`.

The server associates the logical operation with the resource created by
that operation.

This demonstrates the idempotency mental model, but the guarantee is
limited to application state held by a single running instance.

## 03 - Database-Enforced Idempotency

Example:
[`Idempotency.Database.Api`](./03-Database/Idempotency.Database.Api/) ·
[`Integration Tests`](./03-Database/Idempotency.Database.IntegrationTests/)
· [`compose.yaml`](./03-Database/compose.yaml) ·
[`init.sql`](./03-Database/init.sql)

Moves the idempotency invariant to PostgreSQL.

The database becomes responsible for atomically enforcing uniqueness of
the operation identifier.

This lab will explore concurrent requests and multiple application
instances sharing the same persistence layer.

### Refactored Implementation Flow

The final database-backed lab also serves as a small refactoring
exercise. The HTTP layer delegates the use case to `ExpenseService`; the
service defines the transaction boundary through a Unit of Work;
transaction-bound repositories perform the writes; and a separate
`IdempotencyReader` recovers the winning expense after a concurrent
request loses the race and rolls back.

The refactoring does not provide the idempotency guarantee by itself.
The guarantee still comes from the shared PostgreSQL uniqueness
constraint. The structure makes that behavior easier to understand by
keeping HTTP, application orchestration, transaction management,
persistence, and recovery responsibilities explicit.

``` mermaid
sequenceDiagram
    autonumber
    actor Client
    participant A as API Instance A (:5132)
    participant B as API Instance B (:5133)
    participant S as ExpenseService
    participant U as UnitOfWork
    participant ER as ExpenseRepository
    participant IR as IdempotencyRepository
    participant R as IdempotencyReader
    participant DB as PostgreSQL

    par Same request + Idempotency-Key K
        Client->>A: POST /expenses (K, request)
        A->>S: CreateAsync(request, K)
    and
        Client->>B: POST /expenses (K, request)
        B->>S: CreateAsync(request, K)
    end

    rect rgb(235, 250, 235)
        Note over A,DB: Instance A wins the race
        S->>U: Create transaction
        S->>ER: InsertAsync(E1)
        ER->>DB: INSERT expense E1
        S->>IR: InsertAsync(K, E1)
        IR->>DB: INSERT idempotency key K -> E1
        DB-->>IR: OK
        S->>U: CommitAsync()
        U->>DB: COMMIT
        S-->>A: Result(E1, AlreadyExisted=false)
        A-->>Client: 201 Created (E1)
    end

    rect rgb(255, 240, 240)
        Note over B,DB: Instance B loses the race
        S->>U: Create transaction
        S->>ER: InsertAsync(E2)
        ER->>DB: INSERT expense E2
        S->>IR: InsertAsync(K, E2)
        IR->>DB: INSERT idempotency key K -> E2
        DB-->>IR: Unique violation
        Note over IR,S: Translated to IdempotencyConflictException
        S->>U: RollbackAsync()
        U->>DB: ROLLBACK
        S->>R: FindExpenseAsync(K)
        R->>DB: SELECT winning expense for K
        DB-->>R: E1
        R-->>S: E1
        S-->>B: Result(E1, AlreadyExisted=true)
        B-->>Client: 200 OK (E1)
    end
```

The sequence highlights two separate concerns: PostgreSQL selects the
winner by enforcing the unique idempotency key, while the refactored
application structure keeps the use case independent from
PostgreSQL-specific transaction and exception details. Both callers
ultimately observe the same logical expense.

## Learning Progression

The labs intentionally preserve intermediate implementations:

``` text
    identical payload
          |
          v
    01 - Naive
          |
          | introduce operation identity
          v
    02 - In-Memory
          |
          | move invariant to persistence
          v
    03 - Database
```

The goal is not only to show the final implementation, but also to make
the problem and the evolution of its guarantees executable.

## Running the Labs

### 01 - Naive

``` powershell
dotnet run --project src/DeepDives/Idempotency/01-Naive/Idempotency.Naive.Api
```

### 02 - In-Memory

``` powershell
dotnet run --project src/DeepDives/Idempotency/02-InMemory/Idempotency.InMemory.Api
```

### 03 - Database

Start PostgreSQL first:

``` powershell
docker compose -f src/DeepDives/Idempotency/03-Database/compose.yaml up -d
```

Run two API instances against the same database:

``` powershell
dotnet run --project src/DeepDives/Idempotency/03-Database/Idempotency.Database.Api --urls "http://localhost:5132"
```

``` powershell
dotnet run --project src/DeepDives/Idempotency/03-Database/Idempotency.Database.Api --urls "http://localhost:5133"
```

### Concurrent Same-Key Test

The final scenario verifies the distributed guarantee by sending
concurrent requests with the same `Idempotency-Key` to both API
instances.

Run the PowerShell scenario:

``` powershell
./request.same.keyidempotency.paraller.ps1
```

The expected behavior is that one request wins the database race and
commits the expense and idempotency key. The other request receives the
unique-key conflict, rolls back its transaction, reads the winning
expense, and returns that same logical resource.

The important assertion is not which API instance wins. Both responses
must resolve to the same expense identifier, demonstrating that the
guarantee is enforced by the shared PostgreSQL constraint rather than by
process-local state.
