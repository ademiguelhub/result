namespace FluentRecordResults.Extensions;

/// <summary>
/// Extensions to extract the value from a <see cref="Result{T}"/> as an
/// escape hatch out of the result pipeline, throwing on failure.
/// </summary>
public static class ResultGetExtensions
{
    extension<T>(Result<T> result)
    {
        /// <summary>
        /// Gets the value of a successful result or throws an exception if the result is a failure.
        /// </summary>
        /// <param name="onFailure">Optional factory to create a custom exception based on the failed result.</param>
        /// <returns>The value of the result if successful.</returns>
        public T GetOrThrow(Func<Result<T>, Exception>? onFailure = null)
        {
            if (result.IsSuccess) return result.Value!;

            if (onFailure is not null) throw onFailure(result);

            throw result.Code switch
            {
                Error.InvalidInput => new ArgumentException(result.Reason ?? "Invalid input."),
                Error.NotFound => new KeyNotFoundException(result.Reason ?? "Resource not found."),
                Error.DbException => new InvalidOperationException(result.Reason ?? "Database exception occurred."),
                Error.SerializationError => new SerializationException(result.Reason ?? "Error during serialization or deserialization."),
                _ => new ApplicationException(result.Reason ?? "An error occurred.")
            };
        }
    }
}
