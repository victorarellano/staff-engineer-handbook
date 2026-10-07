# Idempotency Labs

These labs progressively explore the guarantees required to make backend operations idempotent.

They accompany:

`docs/deep-dives/integration/idempotency.md`

## 01 - Naive

Example: [`Idempotency.Naive.Api`](./01-Naive/Idempotency.Naive.Api/) ·
[`Integration Tests`](./01-Naive/Idempotency.Naive.IntegrationTests/)

Demonstrates the initial problem.

Two HTTP requests containing the same payload are treated as two independent operations and therefore create two different expenses.

The server has no information that allows it to distinguish a retry from a new business operation.

## 02 - In-Memory Idempotency

Example:
[`Idempotency.InMemory.Api`](./02-InMemory/Idempotency.InMemory.Api/) ·
[`Integration Tests`](./02-InMemory/Idempotency.InMemory.IntegrationTests/)

Introduces a client-generated `Idempotency-Key`.

The server associates the logical operation with the resource created by that operation.

This demonstrates the idempotency mental model, but the guarantee is limited to application state held by a single running instance.

## 03 - Database-Enforced Idempotency

Example:
[`Idempotency.Database.Api`](./03-Database/Idempotency.Database.Api/) ·
[`Integration Tests`](./03-Database/Idempotency.Database.IntegrationTests/)
· [`compose.yaml`](./03-Database/compose.yaml) ·
[`init.sql`](./03-Database/init.sql)

Moves the idempotency invariant to PostgreSQL.

The database becomes responsible for atomically enforcing uniqueness of the operation identifier.

This lab will explore concurrent requests and multiple application instances sharing the same persistence layer.

### Refactored Implementation Flow

The final database-backed lab also serves as a small refactoring exercise. The HTTP layer delegates the use case to `ExpenseService`; the service defines the transaction boundary through a Unit of Work; transaction-bound repositories perform the writes; and a separate `IdempotencyReader` recovers the winning expense after a concurrent request loses the race and rolls back.

The refactoring does not provide the idempotency guarantee by itself. The guarantee still comes from the shared PostgreSQL uniqueness constraint. The structure makes that behavior easier to understand by keeping HTTP, application orchestration, transaction management, persistence, and recovery responsibilities explicit.

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

The sequence highlights two separate concerns: PostgreSQL selects the winner by enforcing the unique idempotency key, while the refactored application structure keeps the use case independent from PostgreSQL-specific transaction and exception details. Both callers ultimately observe the same logical expense.

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
          |
          | atomically persist the intent to publish
          v
    04 - Transactional Outbox

```

The goal is not only to show the final implementation, but also to make the problem and the evolution of its guarantees executable.

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

#### Concurrent Same-Key Test

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

### 04 - Transactional Outbox

Start PostgreSQL first:

``` powershell
docker compose -f src/DeepDives/Idempotency/03-Database/compose.yaml up -d
```

Add Rabbit client in solutions

``` powershell
dotnet add package RabbitMQ.Client
```

Run two API instances against the same database:

``` powershell
dotnet run --project src/DeepDives/Idempotency/04-TransactionalOutbox/Idempotency.TransactionalOutbox.Api --urls "http://localhost:5132"
```

#### Executed Scenarios

The lab was built incrementally. Each scenario isolates a failure mode or concurrency problem that appears when a database transaction must eventually produce a message in an external broker.

##### Scenario 1 - Happy Path

**Purpose:** verify the basic Transactional Outbox flow.

The API creates the business data, the idempotency record, and the outbox message in the same PostgreSQL transaction. The background worker later reads the pending outbox message, publishes it to RabbitMQ, waits for publisher confirmation, and finally sets `published_at`.

Expected result:

``` text
PostgreSQL transaction
    -> Expense
    -> Idempotency Key
    -> Outbox Message (pending)

Outbox Worker
    -> RabbitMQ publish
    -> Publisher Confirm
    -> Outbox Message (published)
```

This demonstrates the central guarantee of the pattern: the business operation and the intention to publish are persisted atomically.

##### Scenario 2 - RabbitMQ Unavailable

**Purpose:** verify that a broker outage does not lose the intention to publish.

RabbitMQ is stopped before the worker can publish. The API can still commit the business transaction and leave the outbox message with `published_at = NULL`.

When RabbitMQ becomes available again, the worker retries the pending message and completes it.

Expected result:

``` text
Business transaction committed
    -> RabbitMQ unavailable
    -> Outbox remains pending
    -> RabbitMQ recovers
    -> Worker retries
    -> Message published
```

The important property is that broker availability is not required to commit the business operation.

##### Scenario 3 - Crash After Publish

**Purpose:** reproduce the failure window between broker confirmation and updating PostgreSQL.

The worker publishes the message successfully to RabbitMQ and then crashes before executing `MarkAsPublishedAsync`.

RabbitMQ already contains the message, while PostgreSQL still reports it as pending. After the worker restarts, the message is published again.

Expected result:

``` text
RabbitMQ accepts M1
    -> worker crashes
    -> PostgreSQL still says pending
    -> worker restarts
    -> M1 is published again
```

This demonstrates why the implementation provides **at-least-once publication**, not exactly-once publication. A stable `message_id` identifies both publications as the same logical message, but it does not prevent the duplicate by itself.

##### Scenario 4 - Multiple Workers Without Coordination

**Purpose:** reproduce the race that occurs when multiple application instances run their own Outbox Worker.

A single pending outbox message is created and two API instances are started. Without a claiming mechanism, both workers can read the same pending row before either one marks it as published.

Expected failure:

``` text
Worker A -> reads M1 -> publishes M1
Worker B -> reads M1 -> publishes M1
```

RabbitMQ receives two copies of the same logical message.

This demonstrates that `published_at IS NULL` is not sufficient to coordinate concurrent workers.

##### Scenario 5 - Multiple Workers With Temporary Reservation

**Purpose:** prevent simultaneous workers from processing the same pending message.

The worker replaces the passive pending-message read with an atomic claim operation. PostgreSQL selects available rows using `FOR UPDATE SKIP LOCKED` and sets `claimed_until` before the transaction commits.

Expected result:

``` text
Worker A -> claims M1 -> publishes M1
Worker B -> skips M1
```

`claimed_until` acts as a temporary reservation. If the worker dies after claiming a message, the reservation eventually expires and another worker can reclaim it.

This solves the concurrent-worker race without keeping a PostgreSQL row lock open while communicating with RabbitMQ.

#### Guarantees Reached by the Lab

The final implementation demonstrates these boundaries:

``` text
Database transaction
    -> atomically persists business state + outbox message

Temporary reservation
    -> prevents simultaneous processing by multiple workers

Publisher confirmation
    -> confirms that RabbitMQ accepted the publication

Retry of pending messages
    -> favors message delivery over silent loss
```

The resulting publication model is **at-least-once**: the design tries to avoid losing a message, while accepting that a duplicate can still occur if the worker crashes after RabbitMQ accepts the message but before PostgreSQL records it as published.

Further production-oriented improvements such as retry backoff, poison-message handling, lease renewal, large-batch processing, outbox retention, and observability are intentionally left for a later iteration.
