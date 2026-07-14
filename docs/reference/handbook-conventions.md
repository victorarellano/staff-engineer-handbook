# Handbook Conventions

This document defines the standards and conventions used throughout the Staff Engineer Handbook project.

---

# Purpose

The purpose of this repository is to build a personal knowledge base that helps validate the technical skills expected from a Senior or Staff Backend Engineer.

The handbook is intended for learning, interview preparation, and long-term reference.

---

# Language

- All documentation must be written in English.
- Discussions and mentoring sessions may be conducted in Spanish.
- Source code must follow standard C# naming conventions.

---

# Repository Structure

```text
docs/
    reference/
    level-1-backend-foundations/
    level-2-web-apis/
    level-3-data-access/
    level-4-cloud/
    level-5-architecture/
    level-6-staff-engineering/

src/
scripts/
```

---

# Question Format

Every interview question must contain exactly three sections.

## Question

The interview question exactly as it could be asked.

## Answer

A concise explanation.

The objective is to answer an interview, not to write a book.

## Example

A complete executable example written in C#.

Every example must compile.

---

# Source Code

Each question must have its own executable example.

Example:

```text
Q001ValueTypesVsReferenceTypes.cs
Q002GarbageCollection.cs
Q003ValueTypesAlwaysLiveOnStack.cs
```

Every example must be executable from Program.cs.

---

# Naming Convention

Questions use a three-digit identifier.

Examples:

```text
001
002
003
...
125
```

Documentation:

```text
001-value-types-vs-reference-types.md
```

Source code:

```text
Q001ValueTypesVsReferenceTypes.cs
```

---

# Quality Rules

Every question must:

- compile successfully;
- provide a runnable example;
- demonstrate a single concept;
- avoid unnecessary complexity;
- avoid introducing unrelated concepts.

---

# Examples

Examples should demonstrate one concept only.

Good:

- Garbage Collection
- async/await
- Boxing
- Dependency Injection

Avoid mixing multiple concepts in the same example.

---

# References

Answers should be based on reliable technical sources whenever possible.

Preferred references include:

- Microsoft Learn
- Microsoft Documentation
- C# Language Specification
- .NET Runtime Documentation
- Official product documentation
- Well-established technical authors

---

# Git Workflow

Recommended workflow:

```bash
git status
git pull

git add .
git commit -m "<type>: <description>"

git push
```

---

# Commit Convention

Recommended commit prefixes:

| Prefix | Purpose |
|---------|----------|
| feat | New functionality |
| docs | Documentation |
| fix | Bug fix |
| refactor | Internal improvements |
| test | Tests |
| chore | Maintenance |

---

# Long-Term Goal

The handbook should remain:

- simple;
- practical;
- executable;
- easy to review before an interview;
- based on verified technical knowledge.

The objective is quality rather than quantity.