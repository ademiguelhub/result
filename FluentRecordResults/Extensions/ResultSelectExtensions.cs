namespace FluentRecordResults.Extensions;

/// <summary>
/// Extensions to map the carried value of a <see cref="Result{T}"/> to a new
/// value while propagating failure (functor map).
/// </summary>
public static class ResultSelectExtensions
{
    extension<T>(Result<T> result)
    {
        /// <summary>
        /// Maps the successful result of a Result to another value.
        /// If the result is a failure, the failure is propagated to the returned Result.
        /// </summary>
        /// <typeparam name="TDest">The type of the output result value.</typeparam>
        /// <param name="select">The mapping function to apply on success.</param>
        /// <returns>The mapped result if successful; otherwise, a failed result with the same error code and message.</returns>
        public Result<TDest> Select<TDest>(Func<T, TDest> select) =>
            result.IsSuccess
                ? Result<TDest>.Ok(select(result.Value!))
                : Result<TDest>.Fail(result.Code, result.Reason);
    }

    extension<T>(Task<Result<T>> task)
    {
        /// <summary>
        /// Maps the successful result of a task-wrapped Result to another value.
        /// </summary>
        /// <typeparam name="TDest">The type of the output result value.</typeparam>
        /// <param name="select">The mapping function to apply on success.</param>
        /// <returns>A task containing the mapped result.</returns>
        public async Task<Result<TDest>> SelectAsync<TDest>(Func<T, TDest> select)
        {
            var res = await task;
            return res.IsSuccess
                ? Result<TDest>.Ok(select(res.Value!))
                : Result<TDest>.Fail(res.Code, res.Reason);
        }
    }
}
