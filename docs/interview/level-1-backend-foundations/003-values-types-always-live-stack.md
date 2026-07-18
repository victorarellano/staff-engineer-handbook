### 003 - Do value types always live on the stack?
## Question

Do value types always live on the stack?

## Answer

No.

This is a common misconception.

Value types are defined by their copy semantics, not by where they are physically stored.

Their actual storage location depends on the execution context and the optimizations performed by the .NET runtime.

For example:

- A local value type is commonly stored on the stack.
- A value type that is a field of a class is stored inside the object on the managed heap.
- The JIT compiler may keep a value type entirely in CPU registers.
- Boxing a value type creates an object on the managed heap.

Therefore, "value type" does not mean "stored on the stack".

The important characteristic is that value types are copied by value.

## Example

Source code:

[Q003ValueTypesAlwaysLiveStack.cs](../../../src/Level1.BackendFoundations/Examples/Q003ValueTypesAlwaysLiveStack.cs)

Run from the repository root:

```powershell
dotnet run --project src/Level1.BackendFoundations -- 003
```