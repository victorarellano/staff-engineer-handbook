# Staff Engineering Handbook

> Learn deeply. Build confidently. Explain clearly.

An engineering handbook built through executable examples, architecture discussions, deep technical explorations, and continuous learning.

The purpose of this repository is to consolidate engineering knowledge while preparing for Senior, Staff, and Principal Engineering roles. Rather than collecting isolated notes, the handbook connects theory, architecture, implementation, and hands-on practice.

---

# Learning Paths

The repository has two complementary learning paths.

## 1. Interview Preparation

A structured progression through engineering topics commonly explored in technical interviews. The path is organized into levels, from backend foundations to Staff-level engineering topics.

| Level | Area |
|------:|------|
| 1 | Backend Foundations |
| 2 | Web APIs |
| 3 | Data Access |
| 4 | Cloud |
| 5 | Architecture |
| 6 | Staff Engineering |

Documentation lives under [`docs/interview`](docs/interview/), with executable examples under `src/Interview/`.

## 2. Deep Dives

In-depth explorations of engineering problems where the goal is not only to know how to implement a solution, but to understand why it works, which guarantees it provides, what can fail, and which trade-offs exist.

Deep Dive knowledge lives under [`docs/deep-dives`](docs/deep-dives/), while executable experiments and labs live under [`src/DeepDives`](src/DeepDives/).

A Deep Dive may include conceptual documentation, diagrams, failure scenarios, experiments, implementation alternatives, and executable labs.

---

# Repository Structure

The repository separates knowledge from executable learning material while keeping both organized around the same learning paths.

```text
docs/
├── interview/              # Interview preparation knowledge
│   ├── level-1-backend-foundations/
│   ├── level-2-web-apis/
│   ├── level-3-data-access/
│   ├── level-4-cloud/
│   ├── level-5-architecture/
│   └── level-6-staff-engineering/
│
├── deep-dives/             # Deep technical knowledge
│   ├── concurrency/
│   ├── database/
│   ├── dotnet-runtime/
│   ├── integration/
│   ├── kubernetes/
│   ├── networking/
│   └── security/
│
└── reference/              # Supporting reference material

src/
├── Interview/              # Executable interview examples
│   ├── Level1.BackendFoundations/
│   ├── Level2.WebApis/
│   ├── Level3.DataAccess/
│   ├── Level4.Cloud/
│   ├── Level5.Architecture/
│   └── Level6.StaffEngineering/
│
└── DeepDives/              # Executable experiments and labs
    ├── Concurrency/
    └── Idempotency/
```

The convention is intentionally simple:

- `docs/` explains the concepts, reasoning, guarantees, and trade-offs.
- `src/` contains executable examples used to observe and validate those concepts.

---

# Current Content

## Deep Dives

| Area | Topic | Documentation | Executable Labs | Status |
|------|-------|---------------|-----------------|--------|
| Concurrency | Concurrency and coordination | [Documentation](docs/deep-dives/concurrency/README.md) | [Labs](src/DeepDives/Concurrency/) | Completed |
| Integration | Idempotency | [Documentation](docs/deep-dives/integration/idempotency/README.md) | [Labs](src/DeepDives/Idempotency/) | Completed |
| Security | Security fundamentals and protocols | [Documentation](docs/deep-dives/security/README.md) | — | In Progress |

Additional Deep Dive areas will be added progressively as the handbook evolves.

## Interview Preparation

### Level 1 – Backend Foundations

| Number | Topic | Status |
|------:|-------|--------|
| 001 | [Value Types vs Reference Types](docs/interview/level-1-backend-foundations/001-value-types-vs-reference-types.md) | Completed |
| 002 | [Garbage Collection](docs/interview/level-1-backend-foundations/002-garbage-collection.md) | Completed |
| 003 | [Do Value Types Always Live on the Stack?](docs/interview/level-1-backend-foundations/003-values-types-always-live-stack.md) | Completed |

---

# Getting Started

## Requirements

- .NET 8 SDK
- Git
- Docker when required by a specific lab

Build the solution from the repository root:

```powershell
dotnet build
```

Individual Deep Dives and interview examples contain their own execution instructions when additional infrastructure or commands are required.

---

# Reference Material

Supporting material used throughout the handbook lives under [`docs/reference`](docs/reference/).

This area contains material such as architecture references, diagrams, cheat sheets, reference tables, and useful links that support the main learning paths.

---

# Learning Philosophy

Software engineering is not about memorizing technologies.

It is about understanding **why technologies exist**, **which problems they solve**, **which guarantees they provide**, and **how they behave in real systems**.

Every topic in this handbook follows the same engineering learning process:

1. Understand the problem.
2. Learn the underlying concepts.
3. Explore the internal architecture.
4. Visualize the solution using diagrams.
5. Build executable examples.
6. Reproduce relevant failure scenarios.
7. Validate the behavior and guarantees.
8. Document the knowledge and trade-offs.

The goal is to develop **engineering intuition** instead of collecting isolated facts.

This repository is intended to evolve over time into an engineering handbook that connects theory, architecture, implementation, experimentation, and practical experience.
