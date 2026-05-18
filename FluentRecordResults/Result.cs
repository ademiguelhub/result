namespace FluentRecordResults;

/// <summary>
/// Standard result error codes used by service and repository operations.
/// </summary>
public enum Error
{
    /// <summary>No error.</summary>
    None,
    /// <summary>Generic error.</summary>
    Error,
    /// <summary>Invalid input provided by the caller.</summary>
    InvalidInput,
    /// <summary>Requested resource not found.</summary>
    NotFound,
    /// <summary>Operation was cancelled by the caller.</summary>
    Cancelled,
    /// <summary>Operation timed out before completing.</summary>
    Timeout,
    /// <summary>Database related exception occurred.</summary>
    DbException,
    /// <summary>Error during serialization or deserialization.</summary>
    SerializationError,
}

/// <summary>
/// Lightweight non-generic result indicating success/failure and an optional message.
/// </summary>
/// <param name="IsSuccess">Indicates whether the operation succeeded.</param>
/// <param name="Code">Optional error code describing the failure.</param>
/// <param name="Reason">Optional human-readable message for failures.</param>
public record Result(bool IsSuccess, Error Code = Error.None, string? Reason = null)
{
    /// <summary>Create a successful result.</summary>
    /// <param name="reason">Optional human-readable message attached to the result.</param>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Ok(string? reason = null) => new(true, Error.None, reason);

    /// <summary>Create a failed result.</summary>
    /// <param name="code">The error code describing the failure.</param>
    /// <param name="reason">Optional human-readable message describing the failure.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Fail(Error code, string? reason = null) => new(false, code, reason);

    /// <summary>Indicates whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Allow using a <see cref="Result"/> directly in boolean contexts (e.g. if (result)).</summary>
    public static implicit operator bool(Result r) => r.IsSuccess;
}

/// <summary>
/// Generic result that carries a value on success.
/// </summary>
/// <typeparam name="T">Type of the value carried on success.</typeparam>
/// <param name="IsSuccess">Indicates whether the operation succeeded.</param>
/// <param name="Value">The value returned on success.</param>
/// <param name="Code">Optional error code describing the failure.</param>
/// <param name="Reason">Optional human-readable message for failures.</param>
public record Result<T>(bool IsSuccess, T? Value, Error Code = Error.None, string? Reason = null) : Result(IsSuccess, Code, Reason)
{
    /// <summary>Create a successful result carrying <paramref name="value"/>.</summary>
    /// <param name="value">The value to carry on the successful result.</param>
    /// <param name="reason">Optional human-readable message attached to the result.</param>
    /// <returns>A successful <see cref="Result{T}"/> wrapping <paramref name="value"/>.</returns>
    public static Result<T> Ok(T value, string? reason = null) =>
        new(true, value, Error.None, reason);

    /// <summary>Create a failed result. The carried value is <c>default</c>.</summary>
    /// <param name="code">The error code describing the failure.</param>
    /// <param name="reason">Optional human-readable message describing the failure.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    new public static Result<T> Fail(Error code, string? reason = null) =>
        new(false, default, code, reason);

    /// <summary>Allow using a <see cref="Result{T}"/> directly in boolean contexts (e.g. if (result)).</summary>
    public static implicit operator bool(Result<T> r) => r.IsSuccess;
}
