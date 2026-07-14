# Staff Engineer Handbook

Personal interview-preparation handbook with concise answers and executable
.NET examples.

Each topic contains:

1. Question
2. Answer
3. Executable example

## Requirements

- .NET 8 SDK
- Git

## Build

```powershell
dotnet build
```

## List examples

```powershell
dotnet run --project src/Level1.BackendFoundations
```

## Run an example

```powershell
dotnet run --project src/Level1.BackendFoundations -- 001
```

## Roadmap

| Level | Area | Status |
|---:|---|---|
| 1 | Backend Foundations | In progress |
| 2 | Web APIs | Planned |
| 3 | Data Access | Planned |
| 4 | Cloud | Planned |
| 5 | Architecture | Planned |
| 6 | Staff Engineering | Planned |

## Level 1 - Backend Foundations

| Number | Question | Status |
|---:|---|---|
| 001 | [Value Types vs Reference Types](docs/level-1-backend-foundations/001-value-types-vs-reference-types.md) | Completed |
| 002 | [Garbage Collection](docs/level-1-backend-foundations/002-garbage-collection.md) | Completed |
| 003 | [Value Type Always Live Stack](docs/level-1-backend-foundations/003-values-types-always-live-stack.md) | Completed |



## Interview Tip
Many developers answer:

> "Value types live on the stack."

A more accurate answer is:

> "Value types have value semantics. Their physical storage depends on the execution context and runtime optimizations."
