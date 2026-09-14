# DD-005 — Exception Handling Lab

> **Objective**
>
> Validate the behavior of the .NET runtime through small experiments.
> The goal is not to learn new concepts, but to observe how Exception Handling, the CLR, and the Garbage Collector actually behave.

---

# Prerequisites

Before starting this lab you should understand:

- Call Stack
- Stack Frames
- Exception propagation
- try / catch / finally
- throw vs throw ex
- IDisposable
- using
- Managed vs Unmanaged Resources

---

# Lab 1 — Exception Propagation

## Goal

Observe how an exception propagates through the Call Stack.

### Exercise

Create the following methods:

```
Main
 ↓
A
 ↓
B
 ↓
C
```

Where `C()` throws an exception.

### Questions

- Which methods finish normally?
- Which Stack Frames are removed?
- Where does the CLR stop?

---

# Lab 2 — throw vs throw ex

## Goal

Observe the difference in the StackTrace.

### Exercise

Catch the exception inside `B`.

Execute two versions:

Version 1

```
throw;
```

Version 2

```
throw ex;
```

### Compare

- Exception.Message
- Exception.StackTrace

### Questions

- Which StackTrace preserves the original throw location?
- Why?

---

# Lab 3 — finally

## Goal

Verify that finally always executes.

### Scenarios

- Normal execution
- return
- Exception

### Questions

- Does finally execute in every scenario?
- At what moment?

---

# Lab 4 — using

## Goal

Understand that using is syntactic sugar.

### Exercise

Replace

```
using (...)
{
}
```

with its equivalent

```
try
{
}
finally
{
    Dispose();
}
```

### Questions

- Does the behavior change?
- Which version does the compiler generate?

---

# Lab 5 — IDisposable

## Goal

Observe when Dispose() executes.

### Exercise

Create a class implementing IDisposable.

Log:

- Constructor
- Dispose()

Execute inside a using block.

### Questions

- When is Dispose called?
- Who calls it?

---

# Lab 6 — Forgetting Dispose

## Goal

Understand why Dispose exists.

### Exercise

Open a FileStream.

Do not call Dispose.

Try to delete or reopen the file.

Repeat using a using block.

### Questions

- What changes?
- Why?

---

# Lab 7 — Finalizer (Optional)

## Goal

Observe how Finalizers behave.

### Exercise

Create a class with:

- Constructor
- Finalizer
- Dispose

Force a GC using:

```
GC.Collect();
GC.WaitForPendingFinalizers();
```

### Questions

- Does the Finalizer execute immediately?
- Does Dispose prevent the Finalizer?
- Why is the Finalizer considered a safety net instead of the normal flow?

---

# Key Takeaways

- Exceptions change the execution flow.
- The CLR performs Stack Unwinding.
- throw preserves the original propagation.
- throw ex starts a new propagation.
- finally guarantees cleanup.
- using is syntactic sugar over try/finally.
- Dispose releases unmanaged resources.
- The Garbage Collector releases managed memory.
- Finalizers are a last-resort safety mechanism.

---
