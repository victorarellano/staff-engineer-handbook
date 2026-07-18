# Handbook Conventions

This document defines the standards, conventions, and engineering principles used throughout the **Staff Engineering Handbook**.

The objective is to keep the repository consistent, maintainable, and easy to navigate as it grows over time.

---

# Purpose

The Staff Engineering Handbook is a living engineering knowledge base designed to:

- develop deep technical understanding;
- validate engineering concepts through executable examples;
- prepare for Senior, Staff, and Principal Engineering roles;
- serve as a long-term technical reference.

The handbook prioritizes understanding over memorization.

---

# Language

- All documentation must be written in English.
- Discussions and mentoring sessions may be conducted in Spanish.
- Source code must follow standard language conventions.
- Public documentation should be clear, concise, and technically accurate.

---

# Repository Structure

```text
docs/

    interview/
        level-1-backend-foundations/
        level-2-web-apis/
        level-3-data-access/
        level-4-cloud/
        level-5-architecture/
        level-6-staff-engineering/

    deep-dives/
        networking/
        security/
        kubernetes/
        docker/
        cloud/
        architecture/
        databases/
        dotnet-runtime/
        observability/

    reference/

src/

scripts/
```

---

# Content Types

The repository contains four different kinds of documents.

## Interview Questions

Designed for interview preparation.

Each question contains:

- Question
- Answer
- Technical explanation
- Executable example

---

## Deep Dives

Designed for deep technical learning.

Every Deep Dive should follow the standard template:

- Objective
- Why does it exist?
- Problem it solves
- Key concepts
- High-level overview
- Internal architecture
- Step-by-step explanation
- Diagrams
- Real-world examples
- Implementation examples
- Common misconceptions
- Best practices
- Summary
- References

---

## Hands-on Labs

Executable projects used to validate engineering concepts.

Examples include:

- Console Applications
- ASP.NET APIs
- Docker
- Kubernetes
- Distributed Systems

---

## Reference

Supporting documentation.

Examples include:

- ADRs
- Cheat Sheets
- Diagrams
- Notes
- Tables
- External References

---

# Naming Conventions

## Interview Questions

Questions use a three-digit identifier.

Examples

```text
001
002
003
...
```

Documentation

```text
001-value-types-vs-reference-types.md
```

Source Code

```text
Q001ValueTypesVsReferenceTypes.cs
```

---

## Deep Dives

Deep Dives also use sequential numbering.

Examples

```text
001-http.md
002-https.md
003-tls-handshake.md
```

---

## Hands-on Labs

Projects should use descriptive names.

Examples

```text
TlsHandshakeDemo

GarbageCollectionLab

KubernetesNetworkingLab
```

---

# Source Code

Whenever possible, concepts should include executable examples.

Examples should:

- compile successfully;
- demonstrate one concept;
- avoid unnecessary complexity;
- be production-quality when practical.

---

# Quality Guidelines

Every document should:

- explain why the technology exists;
- explain the problem it solves;
- explain how it works internally;
- include diagrams whenever they improve understanding;
- avoid assumptions;
- be based on verified technical information.

---

# References

Whenever possible, use official documentation.

Preferred references include:

- Microsoft Learn
- Microsoft Documentation
- Kubernetes Documentation
- Docker Documentation
- RFCs
- C# Language Specification
- .NET Runtime Documentation
- Official product documentation

---

# Git Workflow

Recommended workflow

```bash
git status
git pull

git add .

git commit -m "<type>: <description>"

git push
```

---

# Commit Convention

| Prefix | Purpose |
|---------|----------|
| feat | New functionality |
| docs | Documentation |
| fix | Bug fix |
| refactor | Internal improvements |
| test | Tests |
| chore | Maintenance |

---

# Learning Philosophy

Every engineering topic should follow the same learning process.

1. Understand the problem.
2. Learn the underlying concepts.
3. Explore the internal architecture.
4. Visualize the solution.
5. Build executable examples.
6. Validate the behavior.
7. Document the knowledge.
8. Be able to explain it clearly.

The objective is not to memorize technologies.

The objective is to develop engineering intuition through continuous learning, practical implementation, and clear technical communication.

---

# Long-Term Vision

The handbook should evolve into a complete engineering knowledge base that combines:

- theory;
- implementation;
- executable examples;
- architecture discussions;
- engineering best practices;
- real-world experience.

Quality, clarity, and consistency should always take precedence over quantity.