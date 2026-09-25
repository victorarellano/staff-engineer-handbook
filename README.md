# Staff Engineering Handbook

> Learn deeply. Build confidently. Explain clearly

A engineering handbook built through executable examples, architecture
discussions, deep technical explorations, and continuous learning.

The purpose of this repository is to consolidate engineering knowledge into a
single place while preparing for Senior, Staff, and Principal Engineering roles.

Rather than collecting isolated notes, this handbook connects theory,
architecture, implementation, and hands-on practice.

---

# Repository Structure

The repository is organized into four complementary areas.

## Interview Questions

Concise answers to common engineering interview questions.

Each topic contains:

1. Question
2. Answer
3. Technical explanation
4. Executable .NET example (when applicable)

---

## Deep Dives

In-depth explorations of complex engineering topics.

Each Deep Dive follows a consistent structure:

1. Why does it exist?
2. Which problem does it solve?
3. Internal architecture
4. Step-by-step explanation
5. Diagrams
6. Real-world examples
7. Implementation examples (.NET, Kubernetes, Cloud, etc.)
8. Common misconceptions
9. Summary
10. References

Example topics include:

- HTTP / HTTPS
- TLS & mTLS
- PKI
- OAuth2 & OpenID Connect
- JWT
- Docker Internals
- Kubernetes
- Distributed Systems
- Garbage Collection
- CLR
- Async/Await
- DNS
- TCP/IP
- Observability
- Cloud Architecture

---

## Hands-on Labs

Executable projects used to validate engineering concepts through code.

Examples include:

- Console Applications
- ASP.NET APIs
- Docker
- Kubernetes
- Cloud
- Distributed Systems
- Architecture Patterns

---

## Reference

Supporting material used throughout the handbook.

Examples include:

- Architecture Decision Records (ADR)
- Diagrams
- Cheat Sheets
- Reference Tables
- Useful Links
- Notes

---

# Requirements

- .NET 8 SDK
- Git

---

# Build

```powershell
dotnet build
```

---

# List Available Examples

```powershell
dotnet run --project src/Level1.BackendFoundations
```

---

# Run an Example

```powershell
dotnet run --project src/Level1.BackendFoundations -- 001
```

---

# Engineering Roadmap

## Learning Levels

| Level | Area | Status |
|------:|------|--------|
| 1 | Backend Foundations | In Progress |
| 2 | Web APIs | Planned |
| 3 | Data Access | Planned |
| 4 | Cloud | Planned |
| 5 | Architecture | Planned |
| 6 | Staff Engineering | Planned |
|   | Concurrency | Completed |

---

## Deep Dive Areas

- Networking
- Security
- Docker
- Kubernetes
- Cloud
- Distributed Systems
- Databases
- Observability
- .NET Runtime
- Software Architecture
- Concurrency

---

## Hands-on Labs

- Backend Foundations
- Web APIs
- Docker
- Kubernetes
- Cloud
- Distributed Systems
- System Design
- Concurrency

---

# Current Content

## Interview Questions

### Level 1 – Backend Foundations

| Number | Topic | Status |
|------:|-------|--------|
| 001 | [Value Types vs Reference Types](docs/level-1-backend-foundations/001-value-types-vs-reference-types.md) | Completed |
| 002 | [Garbage Collection](docs/level-1-backend-foundations/002-garbage-collection.md) | Completed |
| 003 | [Do Value Types Always Live on the Stack?](docs/level-1-backend-foundations/003-values-types-always-live-stack.md) | Completed |

---

## Deep Dives

Deep technical explorations organized by engineering area.

### Concurrency

Concurrency fundamentals, synchronization mechanisms, coordination strategies,
and distributed concurrency patterns explored through executable simulations.

→ [Concurrency Deep Dives](docs/deep-dives/concurrency/README.md)

### Integration

Idempotency explored progressively through executable labs, from the
initial duplicate-operation problem to in-memory coordination and
database-enforced idempotency across multiple API instances.

→ [Idempotency Deep Dive & Labs](src/DeepDives/Idempotency/README.md)

### Other Areas

Additional Deep Dive areas are planned and will be added progressively.

---

## Hands-on Labs

> Growing together with each learning level.

---

# Learning Philosophy

Software engineering is not about memorizing technologies.

It is about understanding **why technologies exist**, **which problems they solve**, and **how they work internally**.

Every topic in this handbook follows the same engineering learning process:

1. Understand the problem.
2. Learn the underlying concepts.
3. Explore the internal architecture.
4. Visualize the solution using diagrams.
5. Build executable examples.
6. Validate the behavior.
7. Document the knowledge.

The goal is to develop **engineering intuition** instead of collecting isolated facts.

This repository is intended to evolve over time into a complete engineering handbook that combines theory, architecture, implementation, and practical experience.