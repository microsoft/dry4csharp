---
name: build-and-test-full
description: Builds dry4csharp and runs the full test suite including integration tests.
---

## Commands

Run from the repository root:

    dotnet build dry4csharp.sln --configuration Release
    dotnet test dry4csharp.sln --configuration Release

## Notes

Integration tests spawn real processes — `git`, the built dry4csharp CLI, and `dotnet`. They require
`git` and the .NET SDK on `PATH`. There are **no secrets** to inject.

## Pass criteria

- Build succeeds with **zero warnings and zero errors** (warnings are errors in Release).
- All tests pass (unit + integration).
