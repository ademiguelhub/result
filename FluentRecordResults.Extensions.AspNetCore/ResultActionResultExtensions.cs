namespace FluentRecordResults.Extensions.AspNetCore;

/// <summary>
/// Extensions for converting <see cref="Result"/> and <see cref="Result{T}"/> to ASP.NET Core action results.
/// Maps <see cref="Error"/> to appropriate HTTP status codes while preserving Result body.
/// </summary>
public static class ResultActionResultExtensions
{
    extension(Result result)
    {
        /// <summary>
        /// Convert a <see cref="Result"/> to an <see cref="IActionResult"/>.
        /// The Result is serialized as the response body.
        /// </summary>
        /// <param name="statusCode">Optional HTTP status code override. When <c>null</c>, the status code is derived from <see cref="Error"/>.</param>
        /// <returns>An <see cref="ObjectResult"/> with the Result as body and the chosen HTTP status code.</returns>
        public IActionResult ToActionResult(int? statusCode = null)
        {
            statusCode ??= GetStatusCode(result);
            return new ObjectResult(result) { StatusCode = statusCode };
        }
    }

    extension<T>(Result<T> result)
    {
        /// <summary>
        /// Convert a <see cref="Result{T}"/> to an <see cref="IActionResult"/>.
        /// The Result is serialized as the response body.
        /// </summary>
        /// <param name="statusCode">Optional HTTP status code override. When <c>null</c>, the status code is derived from <see cref="Error"/>.</param>
        /// <returns>An <see cref="ObjectResult"/> with the Result as body and the chosen HTTP status code.</returns>
        public IActionResult ToActionResult(int? statusCode = null)
        {
            statusCode ??= GetStatusCode(result);
            return new ObjectResult(result) { StatusCode = statusCode };
        }
    }

    /// <summary>
    /// Map a <see cref="Result"/> to an HTTP status code based on its <see cref="Error"/>.
    /// Successful results map to 200; unmapped failure codes fall through to 500.
    /// </summary>
    private static int GetStatusCode(Result result) => result switch
    {
        { IsSuccess: true } => StatusCodes.Status200OK,
        { Code: Error.InvalidInput } => StatusCodes.Status400BadRequest,
        { Code: Error.NotFound } => StatusCodes.Status404NotFound,
        { Code: Error.DbException } => StatusCodes.Status500InternalServerError,
        { Code: Error.SerializationError } => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };
}
