# 005 - Value Types vs Reference Types

## Question

What is the difference between value types and reference types in C#?

## Answer

A value-type variable contains its value directly. Assigning it to another
variable copies the value, so modifying one variable does not modify the other.

A reference-type variable contains a reference to an object. Assigning it to
another variable copies the reference, so both variables can refer to the same
object.

Examples of value types include `int`, `bool`, `enum`, and `struct`.
Examples of reference types include `class`, arrays, delegates, and `string`.

This distinction describes copying and assignment semantics. It does not imply
that every value type is always stored on the stack.

## Example

Source code:

[Q005ValueTypesVsReferenceTypes.cs](../../../src/Level1.BackendFoundations/Examples/Q005ValueTypesVsReferenceTypes.cs)

Run from the repository root:

```powershell
dotnet run --project src/Level1.BackendFoundations -- 005
```
