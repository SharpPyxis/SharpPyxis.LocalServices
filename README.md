# SharpPyxis.LocalServices

Small local HTTP utilities exposed as a lightweight ASP.NET Core API.

This repository contains the local-services half of an older mixed codebase that originally bundled two different concerns in one place:

- a small HTTP utility API intended to run locally;
- SQL Server-specific SQL CLR components.

The split is intentional. `SharpPyxis.LocalServices` keeps the HTTP/API subset as a standalone local tool, while the SQL Server-specific subset now lives in `SharpPyxis.SqlServer`.

## Why this repository exists

Some capabilities are trivial or comfortable to implement in .NET but awkward to host directly inside T-SQL or SQL CLR. Exposing them through a small local API keeps the execution model simple, makes dependencies easier to manage, and still lets SQL-oriented workflows consume these capabilities when needed.

In other words, this repository is not a generic web application platform. It is a pragmatic local toolbelt exposed over HTTP.

## Current scope

The current API surface is intentionally focused. It includes endpoints for:

- data conversions;
- JSON/XML serialization helpers;
- QR code generation;
- image and drawing-related helpers;
- simple security-oriented helpers;
- organization normalization against `recherche-entreprises.api.gouv.fr`.

The host also exposes operational endpoints such as `/version` and `/healthz`.

Swagger is available for local discovery, but it is intentionally restricted to loopback / localhost access.

## Structure

- `src/SharpPyxis.LocalServices.Api/`: API project
- `tests/`: repository-level test area reserved for future use
- `SharpPyxis.LocalServices.slnx`: repository solution

## Build

```powershell
dotnet build .\SharpPyxis.LocalServices.slnx
```

## Run

```powershell
dotnet run --project .\src\SharpPyxis.LocalServices.Api\SharpPyxis.LocalServices.Api.csproj
```

Once the API is running, the most useful entry points are typically:

- `/swagger` for local exploration;
- `/healthz` for a simple health probe;
- `/version` for runtime/version identification.

## Notes

- This repository deliberately stays independent from SQL Server, even though SQL-centric workflows are one of its consumers.
- The goal is to keep the service local, small, and easy to host near the calling application or developer workstation.
- Heavier or unrelated legacy features were intentionally left out during the split.

## License

MIT
