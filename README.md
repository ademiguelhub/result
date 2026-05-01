# FluentRecordResults

A small `Result` / `Result<T>` record type for the result-of-object pattern, with
fluent extensions to chain, map, branch on, and unwrap result values. Host-specific
adapters (ASP.NET Core, …) ship as separate packages so the core stays
framework-agnostic.

> **Status:** pre-release. The library is being extracted from a private project
> into a standalone package; the API may shift before 1.0 and it is not yet
> published to NuGet.

## Packages

| Package                                     | Purpose                                                                |
| ------------------------------------------- | ---------------------------------------------------------------------- |
| `FluentRecordResults`                       | Core: `Result` / `Result<T>`, `Bind`, `Select`, `Match`, `GetOrThrow`. |
| `FluentRecordResults.Extensions.AspNetCore` | ASP.NET Core adapter: `ToActionResult` mapping `Result` → HTTP.        |

Install only what you need:

```bash
dotnet add package FluentRecordResults
dotnet add package FluentRecordResults.Extensions.AspNetCore   # if you're on ASP.NET Core
```

## Requirements

- .NET 10 SDK (the extensions use C# 14 [extension members](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14)).
- `FluentRecordResults.Extensions.AspNetCore` additionally requires the `Microsoft.AspNetCore.App` shared framework.

## API at a glance

Core (`FluentRecordResults`):

| Type / Extension                             | Purpose                                                           |
| -------------------------------------------- | ----------------------------------------------------------------- |
| `Result` / `Result<T>`                       | Success/failure record with `Code` and `Message`; implicit `bool` |
| `ResultErrorCode`                            | Failure taxonomy (`InvalidInput`, `NotFound`, `DbException`, …)   |
| `Bind` / `BindAsync`                         | Chain operations that themselves return a `Result`                |
| `Select` / `SelectAsync`                     | Map the carried value, propagating failure                        |
| `Match` / `MatchAsync` / `MatchAndPropagate` | Pattern-match style dispatch over success / failure               |
| `GetOrThrow`                                 | Escape hatch that throws an exception derived from the error code |

ASP.NET Core (`FluentRecordResults.Extensions.AspNetCore`):

| Extension        | Purpose                                                       |
| ---------------- | ------------------------------------------------------------- |
| `ToActionResult` | Serialize the `Result` as the response body, map code → HTTP. |

`ToActionResult` maps error codes to HTTP status as follows:

| `ResultErrorCode`    | HTTP status |
| -------------------- | ----------- |
| `None` (success)     | 200         |
| `InvalidInput`       | 400         |
| `NotFound`           | 404         |
| `DbException`        | 500         |
| `SerializationError` | 500         |
| `Error` / unmapped   | 500         |

You can override the status code per call: `result.ToActionResult(statusCode: 201)`.

## Example

A controller action that validates input, loads a record, transforms it and
returns the appropriate HTTP response — without manual `if (result.IsFailure)`
plumbing in between:

```csharp
using Results;
using Results.Extensions;

[ApiController]
[Route("orders")]
public class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) =>
        (await ParseId(id)
            .BindAsync(orders.GetByIdAsync)         // Result<Order>
            .SelectAsync(o => OrderDto.From(o)))    // Result<OrderDto>
            .ToActionResult();

    private static Result<int> ParseId(int id) =>
        id > 0
            ? Result<int>.Success(id)
            : Result<int>.Failure(ResultErrorCode.InvalidInput, "Id must be positive.");
}
```

For non-controller code, use `Match` to handle both branches inline:

```csharp
var label = orderResult.Match(
    onSuccess: o   => $"Order #{o.Id} ({o.Total:C})",
    onFailure: r   => $"[{r.Code}] {r.Message}");
```

## Building from source

```bash
dotnet build FluentRecordResults.slnx
dotnet pack  FluentRecordResults.slnx -c Release -o artifacts
```

## License

Released under the [MIT License](https://github.com/ademiguelhub/result/blob/main/LICENSE).

## Author

Andrés de Miguel — [GitHub](https://github.com/ademiguelhub)
