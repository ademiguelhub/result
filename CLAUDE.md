# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project context

This repo is being extracted from a larger ASP.NET project to be published as a standalone NuGet package. The library is pre-1.0 and not yet published to NuGet.

**Package layout (as it will ship):**

- `FluentRecordResults` — base package, framework-agnostic. Contains `Result` / `Result<T>` and the core `Bind` / `Select` / `Match` / `Get` extensions.
- `FluentRecordResults.Extensions.AspNetCore` — separate adapter package that depends on `FluentRecordResults` and adds `ToActionResult` for ASP.NET Core.

The naming convention for future host adapters is `FluentRecordResults.Extensions.<HostName>`. Don't add framework-specific types or `<FrameworkReference>` items to the base package — they belong in their own adapter package.

The C# namespaces are still `Results` / `Results.Extensions` from before the rename. Whether to rename them to match the package IDs is an open pre-1.0 decision (see `PUBLISHING.md` open questions).

## Build & run

Solution file is `FluentRecordResults.slnx` (the new XML `.slnx` format, not `.sln`):

```bash
dotnet build FluentRecordResults.slnx
dotnet build FluentRecordResults/FluentRecordResults.csproj    # single project
dotnet pack  FluentRecordResults.slnx -c Release -o artifacts  # produces every NuGet package
```

There is no test project yet. If asked to add tests, create `FluentRecordResults.Tests/` and add it to `FluentRecordResults.slnx`.

## Architecture

### Core types (`FluentRecordResults/Result.cs`)

- `Result` — non-generic record carrying `IsSuccess`, `ResultErrorCode Code`, `string? Message`. Has static `Success` / `Failure` factories and an implicit `operator bool` so callers can write `if (result) { ... }`.
- `Result<T>` — inherits `Result` and adds `T? Value`. The base `Failure` is hidden with `new` so the generic version returns `Result<T>`.
- `ResultErrorCode` — failure taxonomy (`None`, `Error`, `InvalidInput`, `NotFound`, `DbException`, `SerializationError`). When adding a new code, also extend the `GetStatusCode` switch in `ResultActionResultExtensions` and the `GetOrThrow` switch in `ResultGetExtensions`, otherwise both will silently fall through to their default branches.

### Extension API (`FluentRecordResults/Extensions/`)

Each file groups one operator family. All of them use C# 14's **extension members** syntax (`extension<T>(Result<T> result) { ... }`) — this requires the .NET 10 SDK / `LangVersion` preview-or-later. Don't rewrite as classic `this`-parameter extension methods unless intentionally lowering the language requirement.

Operator families and their convention:

- `ResultBindExtensions` — `Bind` / `BindAsync` (flatMap; chain operations that themselves return `Result`). Stays in the base package.
- `ResultSelectExtensions` — `Select` / `SelectAsync` (map the inner value, propagate failure). Stays in the base package.
- `ResultMatchExtensions` — `Match` / `MatchAsync` / `MatchAndPropagate` (pattern-match dispatch; `*AndPropagate` returns the original result for chaining). Stays in the base package.
- `ResultGetExtensions` — `GetOrThrow` (escape hatch; throws an exception type chosen from `ResultErrorCode`). Stays in the base package.
- `ResultActionResultExtensions` — `ToActionResult` (ASP.NET adapter; maps codes → HTTP status, body is the full `Result`/`Result<T>`). **Will move** to the `FluentRecordResults.Extensions.AspNetCore` package; do not add features to it inside the base project.

The async overloads exist in two flavors that should both be kept in sync when adding new operators: one defined on `Result<T>` (takes a `Func<…, Task<…>>`) and one defined on `Task<Result<T>>` (awaits the source first). Failure propagation pattern is uniform — on failure, construct a fresh failed result with the same `Code` and `Message`; never re-wrap or rethrow.

### Pending split: ASP.NET adapter

`FluentRecordResults.csproj` still uses `<FrameworkReference Include="Microsoft.AspNetCore.App" />` and `GlobalUsings.cs` brings in `Microsoft.AspNetCore.Http` / `Microsoft.AspNetCore.Mvc`. This is required only by `ResultActionResultExtensions` and must move out of the base package before first publish — `PUBLISHING.md` step 2 has the concrete checklist. Until that lands, treat the base project as if it were already framework-agnostic when adding new code: don't introduce new ASP.NET types in any extension file other than `ResultActionResultExtensions`, and don't add new framework-specific `global using` lines.
