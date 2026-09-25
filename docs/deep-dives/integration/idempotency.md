# Idempotency

## Problem

A backend operation may complete successfully while the client never receives the response.

From the client's perspective, the result is unknown. Retrying the request should not execute the same business operation twice.

## Mental Model

It is useful to distinguish three different identities:

### Entity Identity

Identifies the business resource.

Example:

    ExpenseId = 501

It answers:

> What resource am I working with?

### Operation Identity

An idempotency key identifies a logical business operation.

Example:

    OperationId = OP-A

It answers:

> What logical operation am I trying to execute?

### Request Attempt

An HTTP request is only an attempt to execute that logical operation.

A single logical operation may therefore produce multiple HTTP requests.

## Practical Example

A user creates an expense:

    OperationId = OP-A
    Amount      = 120000

The backend successfully persists:

    ExpenseId = 501

but the HTTP response is lost.

The client does not know whether the operation succeeded and retries
using the same idempotency key:

    OP-A
      |
      +-- Attempt 1 -> Expense #501 created -> response lost
      |
      +-- Attempt 2 -> operation already exists
                       -> return Expense #501

The important property is:

    one logical operation
           |
           +-- many possible attempts
           |
           +-- one business effect

The retry must not create Expense #502.

## Persistence Guarantee

If the idempotency key is persisted, the database can enforce its
uniqueness atomically.

For example, in PostgreSQL:

    operation_id UUID NOT NULL UNIQUE

If multiple backend instances attempt to process the same operation concurrently, the database prevents multiple records from being created with the same operation identifier.

This means that an application-level lock or distributed lock is not required merely to guarantee this uniqueness invariant.

## Entity Lifecycle vs Operation Lifecycle

An entity can participate in multiple logical operations during its lifetime.

For example:

    Expense #501
        |
        +-- OP-A -> CREATE $120,000
        |            +-- attempt 1
        |            +-- attempt 2
        |
        +-- OP-B -> UPDATE $150,000
                     +-- attempt 1
                     +-- attempt 2

`Expense #501` identifies the entity.

`OP-A` and `OP-B` identify different logical operations performed on
that entity.

Retries of `OP-B` continue using `OP-B`.

A new business intention receives a new operation identifier.

## Practical Rules

- Same logical intention -> same idempotency key.
- New logical intention -> new idempotency key.
- Same entity does not imply same operation.
- A retry is not a new business operation.
- An HTTP request is an attempt, not necessarily an operation.
- Prefer enforcing an invariant at the persistence boundary when that boundary can guarantee it atomically.

## Connection with Concurrency

A first instinct may be to protect the operation with a distributed lock:

    LOCK(operationId)
        SELECT
        INSERT
    UNLOCK

For this particular invariant, that introduces unnecessary coordination.

If PostgreSQL can enforce:

    UNIQUE(operation_id)

then concurrent backend instances can rely on the database to arbitrate the conflicting writes atomically.

This illustrates a broader engineering heuristic:

> Before introducing distributed coordination, determine whether the
> system that owns the state can enforce the required invariant atomically.

## Topics to Explore

The following topics are intentionally not covered yet:

- Expiration and retention of idempotency keys.
- Reusing an idempotency key with a different payload.
- Persisting and replaying HTTP responses.
- Idempotency across multiple transactional resources.
- Message delivery semantics.
- Out-of-order messages.