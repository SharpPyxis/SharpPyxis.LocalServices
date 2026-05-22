# SharpPyxis.LocalServices

Local HTTP utilities exposed as a small ASP.NET Core API.

This repository hosts local-only services that are useful outside SQL Server as well as from SQL-oriented workflows.

## Structure

- `src/SharpPyxis.LocalServices.Api/`: API project
- `tests/`: test projects reserved for future use
- `SharpPyxis.LocalServices.slnx`: repository solution

## Build

```powershell
dotnet build .\SharpPyxis.LocalServices.slnx
```

## License

MIT
