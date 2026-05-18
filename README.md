# FluentRecordResults

A small `Result` / `Result<T>` record type for the result-of-object pattern, with
fluent extensions to chain, map, branch on, and unwrap result values. Host-specific
adapters (ASP.NET Core, …) ship as separate packages so the core stays
framework-agnostic.

`ToActionResult` and `ServiceClient` are designed as a pair: the server serializes
`Result`-shaped HTTP responses; the client deserializes them back into `Result` objects.
The same type and error codes flow end-to-end across the HTTP boundary, so the full
`Bind` / `Select` / `Match` pipeline is available on both sides.

> **Status:** pre-release. The API may still shift before a stable release.

## Packages

| Package                                     | Version                                                                                                                                                                 | Purpose                                                                |
| ------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- |
| `FluentRecordResults`                       | [![NuGet Version](https://img.shields.io/nuget/v/FluentRecordResults)](https://www.nuget.org/packages/FluentRecordResults/)                                             | Core: `Result` / `Result<T>`, `Bind`, `Select`, `Match`, `GetOrThrow`. |
| `FluentRecordResults.Extensions.AspNetCore` | [![NuGet Version](https://img.shields.io/nuget/v/FluentRecordResults.Extensions.AspNetCore)](https://www.nuget.org/packages/FluentRecordResults.Extensions.AspNetCore/) | ASP.NET Core adapter: `ToActionResult` and `ServiceClient`.            |

Install only what you need:

```bash
dotnet add package FluentRecordResults
dotnet add package FluentRecordResults.Extensions.AspNetCore
```

## Requirements

- .NET 10 SDK (the extensions use C# 14 [extension members](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14)).
- `FluentRecordResults.Extensions.AspNetCore` additionally requires the `Microsoft.AspNetCore.App` shared framework.

## API at a glance

Core (`FluentRecordResults`):

| Type / Extension                             | Purpose                                                                                                                                                                                                                        |
| -------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Result` / `Result<T>`                       | Success/failure record with `Code` and `Reason`; implicit `bool`                                                                                                                                                               |
| `Error`                                      | Open-ended failure taxonomy - codes are inclusive and may grow when new failure kinds are identified. Current codes: `None`, `Error`, `InvalidInput`, `NotFound`, `DbException`, `SerializationError`, `Timeout`, `Cancelled`. |
| `Bind` / `BindAsync`                         | Chain operations that themselves return a `Result`                                                                                                                                                                             |
| `Select` / `SelectAsync`                     | Map the carried value, propagating failure                                                                                                                                                                                     |
| `Match` / `MatchAsync` / `MatchAndPropagate` | Pattern-match style dispatch over success / failure                                                                                                                                                                            |
| `GetOrThrow`                                 | Escape hatch that throws an exception derived from the error code                                                                                                                                                              |

ASP.NET Core (`FluentRecordResults.Extensions.AspNetCore`):

| Type / Extension | Purpose                                                                     |
| ---------------- | --------------------------------------------------------------------------- |
| `ToActionResult` | Serialize the `Result` as the response body and map the error code to HTTP. |
| `ServiceClient`  | Abstract base for typed HTTP clients that expect `Result`-shaped responses. |

### `ToActionResult` status mapping

`ToActionResult` derives the HTTP status from the `Error` code. You can always
override it per call: `result.ToActionResult(statusCode: 201)`.

| `Error`              | HTTP status               |
| -------------------- | ------------------------- |
| `None` (success)     | 200 OK                    |
| `InvalidInput`       | 400 Bad Request           |
| `NotFound`           | 404 Not Found             |
| `DbException`        | 500 Internal Server Error |
| `SerializationError` | 500 Internal Server Error |
| `Timeout`            | 504 Gateway Timeout       |
| `Cancelled`          | 503 Service Unavailable   |
| `Error` / unmapped   | 500 Internal Server Error |

### `ServiceClient`

`ServiceClient` is an abstract base class for typed HTTP clients that talk to
APIs that return `Result`-shaped response bodies. Inherit from it, call the
protected methods, and all network errors (timeouts, cancellations, failed
connections, bad JSON) are captured as `Result` failures - no try/catch in
application code.

Available methods (each returns `Task<Result>` or `Task<Result<TResponse>>`):

| Method                                 | Verb   |
| -------------------------------------- | ------ |
| `GetAsync(path, ct)`                   | GET    |
| `GetAsync<TResponse>(path, ct)`        | GET    |
| `PostAsync<TRequest>(path, body, ct)`  | POST   |
| `PostAsync<TRequest, TResponse>(...)`  | POST   |
| `PutAsync<TRequest>(path, body, ct)`   | PUT    |
| `PutAsync<TRequest, TResponse>(...)`   | PUT    |
| `PatchAsync<TRequest>(path, body, ct)` | PATCH  |
| `PatchAsync<TRequest, TResponse>(...)` | PATCH  |
| `DeleteAsync(path, ct)`                | DELETE |
| `DeleteAsync<TResponse>(path, ct)`     | DELETE |

Override `JsonSerializerOptions` to use a custom serializer for both request
bodies and response deserialization.

**Example - define and register a typed client:**

```csharp
public class OrdersClient(HttpClient httpClient) : ServiceClient(httpClient)
{
    public Task<Result<OrderDto>> GetByIdAsync(int id, CancellationToken ct = default) =>
        GetAsync<OrderDto>($"orders/{id}", ct);

    public Task<Result<OrderDto>> CreateAsync(CreateOrderRequest req, CancellationToken ct = default) =>
        PostAsync<CreateOrderRequest, OrderDto>("orders", req, ct);

    public Task<Result> DeleteAsync(int id, CancellationToken ct = default) =>
        DeleteAsync($"orders/{id}", ct);
}

// Program.cs
builder.Services.AddHttpClient<OrdersClient>(c =>
    c.BaseAddress = new Uri("https://api.example.com/"));
```

## End-to-end example

The typical setup has a backend API returning `Result`-shaped responses via
`ToActionResult`, and a frontend or BFF consuming those responses through a
typed `ServiceClient`. The `Result` type crosses the HTTP boundary intact, so
the full extension pipeline is available on both sides.

### Backend API

```csharp
[ApiController]
[Route("orders")]
public class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) =>
        (await ParseId(id)
            .BindAsync(orders.GetByIdAsync)         // Result<Order>
            .SelectAsync(OrderDto.From))            // Result<OrderDto>
            .ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest req) =>
        (await orders.CreateAsync(req)              // Result<Order>
            .SelectAsync(OrderDto.From))            // Result<OrderDto>
            .ToActionResult(statusCode: 201);

    private static Result<int> ParseId(int id) =>
        id > 0
            ? Result<int>.Ok(id)
            : Result<int>.Fail(Error.InvalidInput, "Id must be positive.");
}
```

### Typed client (Frontend / BFF)

```csharp
public class OrdersClient(HttpClient httpClient) : ServiceClient(httpClient)
{
    public Task<Result<OrderDto>> GetByIdAsync(int id, CancellationToken ct = default) =>
        GetAsync<OrderDto>($"orders/{id}", ct);

    public Task<Result<OrderDto>> CreateAsync(CreateOrderRequest req, CancellationToken ct = default) =>
        PostAsync<CreateOrderRequest, OrderDto>("orders", req, ct);
}

// Program.cs
builder.Services.AddHttpClient<OrdersClient>(c =>
    c.BaseAddress = new Uri("https://api.example.com/"));
```

### Consumer

The client returns the same `Result<T>` the backend produced, so it chains
directly into `Bind`, `Select`, `Match`, or another `ToActionResult`:

```csharp
[ApiController]
[Route("checkout")]
public class CheckoutController(OrdersClient orders) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        CreateOrderRequest req,
        CancellationToken ct) =>
        (await orders.CreateAsync(req, ct)
            .SelectAsync(dto => new OrderConfirmation(dto.Id, dto.Total)))
            .ToActionResult(statusCode: 201);
}
```

For non-controller code, use `Match` to handle both branches inline:

```csharp
var label = orderResult.Match(
    onSuccess: o => $"Order #{o.Id} ({o.Total:C})",
    onFailure: r => $"[{r.Code}] {r.Reason}");
```

## Building from source

```bash
dotnet build FluentRecordResults.slnx
dotnet pack  FluentRecordResults.slnx -c Release -o artifacts
```

## License

Released under the [MIT License](https://github.com/ademiguelhub/result/blob/main/LICENSE).

## Author

Andrés de Miguel - [GitHub](https://github.com/ademiguelhub)
