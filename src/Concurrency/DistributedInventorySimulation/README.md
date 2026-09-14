# Distributed Inventory Simulation

## Prerequisites

- .NET 8 SDK
- Docker

## Start PostgreSQL

From the `DistributedInventorySimulation` directory:

```bash
docker compose up -d
```

This starts the PostgreSQL instance and initializes the inventory table using init.sql.

## Run the simulation
```bash
dotnet run
```

## Stop PostgreSQL
```bash
docker compose down
```

## Reset the database

To remove the PostgreSQL volume and recreate the database from init.sql:

```bash
docker compose down -v
docker compose up -d
```

## PostgreSQL Docker Port Conflict

### Problem

When running the `AtomicDatabaseUpdate` scenario, the application failed with:

```text
28P01: password authentication failed for user "inventory_user"
```

The credentials configured in appsettings.json matched the credentials in docker-compose.yml, so the authentication error was initially unexpected.

Running:
```bash
docker compose ps
```

showed that the expected PostgreSQL container was not running, while:

```text
netstat -ano | findstr :5432
```
showed that port 5432 was already being used by another local PostgreSQL instance.

As a result, the application connection:

```text
Host=localhost;Port=5432
```

was reaching the existing local PostgreSQL server instead of the PostgreSQL container used by the simulation.

### Solution

The Docker PostgreSQL instance was exposed through a different host port:

```yaml
ports:
  - "5433:5432"
```

The application connection string was updated accordingly:

```json
"ConnectionStrings": {
  "InventoryDatabase": "Host=localhost;Port=5433;Database=distributed_inventory;Username=inventory_user;Password=inventory_password"
}
```

The resulting mapping is:

```text
Application
    │
    │ localhost:5433
    ▼
Docker
    │
    │ container:5432
    ▼
PostgreSQL 16
```

After starting the container:

```bash
docker compose up -d
docker compose ps
```

the simulation successfully connected to the Docker database and produced the expected result:

```text
Instance A completed purchase.
Instance B rejected. Not enough stock.

Simulation completed. Successful: 1, Rejected: 1
```

This also isolates the simulation database from any PostgreSQL instance already installed on the development machine.

## Database Evolution for Optimistic Concurrency

The `AtomicDatabaseUpdate` scenario uses the original inventory table created by:

```text
init.sql
```

That schema is intentionally kept unchanged so the previous scenario remains reproducible.

The OptimisticConcurrency scenario requires an additional version column:

```text
version
```

Instead of modifying the original initialization script, the schema evolution is stored in a separate migration-style script:

```text
02-add-optimistic-concurrency.sql
```

### Schema change
```sql
ALTER TABLE product_inventory
ADD COLUMN IF NOT EXISTS version INTEGER NOT NULL DEFAULT 1;
```

This preserves the progression of the exercise:

```text
init.sql
    ↓
AtomicDatabaseUpdate

02-add-optimistic-concurrency.sql
    ↓
OptimisticConcurrency
```

### Apply the schema change

Because PostgreSQL initialization scripts under:

```text
/docker-entrypoint-initdb.d/
```

only run when the database volume is created for the first time, adding a new SQL file does not automatically update an existing database.

To apply the optimistic concurrency change without deleting the current volume, run from the DistributedInventorySimulation directory:

```poweshell
Get-Content .\02-add-optimistic-concurrency.sql |
docker compose exec -T postgres psql `
  -U inventory_user `
  -d distributed_inventory
```

Expected result:

```text
ALTER TABLE
```

### Verify the schema
```powershell
docker compose exec postgres psql `
  -U inventory_user `
  -d distributed_inventory `
  -c "\d product_inventory"
```

The table should contain:
```text
product_id
available_stock
version
```

Run the optimistic concurrency scenario

Once the schema update has been applied:
```bash
dotnet run
```

The scenario should allow both simulated application instances to read the same inventory version, while only one update succeeds.

Example:
```text
Instance A reads stock 1, version 1
Instance B reads stock 1, version 1

Instance A completed purchase.
Instance B concurrency conflict. Version changed.
```

The second update fails because the row version no longer matches the version previously read by that instance.

### Reset the database completely

If the complete database needs to be recreated from scratch:
```bash
docker compose down -v
docker compose up -d
```

After recreating the database, apply the additional optimistic concurrency script again:

```powershell
Get-Content .\02-add-optimistic-concurrency.sql | docker compose exec -T postgres psql ` -U inventory_user ` -d distributed_inventory
```

Keeping schema changes in separate scripts preserves each stage of the exercise and makes the evolution from atomic database updates to optimistic concurrency explicit.