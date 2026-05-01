namespace FluentRecordResults.Extensions;

/// <summary>
/// Extensions for pattern-match style dispatch over <see cref="Result"/> and
/// <see cref="Result{T}"/>. Action-returning overloads are useful for side effects;
/// function-returning overloads let callers map either branch to a value without
/// the null-forgiving operator. <c>MatchAndPropagate</c> returns the original
/// result so the caller can keep chaining.
/// </summary>
public static class ResultMatchExtensions
{
    extension(Result result)
    {
        /// <summary>
        /// Pattern-match dispatch over a non-generic <see cref="Result"/>.
        /// </summary>
        /// <param name="onSuccess">Action invoked on success with the result.</param>
        /// <param name="onFailure">Action invoked on failure with the result.</param>
        public void Match(Action<Result> onSuccess, Action<Result> onFailure)
        {
            if (result.IsSuccess) onSuccess(result);
            else onFailure(result);
        }
    }

    extension(Task<Result> task)
    {
        /// <summary>
        /// Await the source task and pattern-match the awaited <see cref="Result"/>.
        /// </summary>
        /// <param name="onSuccess">Action invoked on success with the result.</param>
        /// <param name="onFailure">Action invoked on failure with the result.</param>
        /// <returns>A task that completes once the matched action returns.</returns>
        public async Task MatchAsync(Action<Result> onSuccess, Action<Result> onFailure)
        {
            var result = await task;
            if (result.IsSuccess) onSuccess(result);
            else onFailure(result);
        }
    }

    extension<T>(Result<T> result)
    {
        /// <summary>
        /// Pattern-match dispatch over a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="onSuccess">Action invoked on success with the carried value.</param>
        /// <param name="onFailure">Action invoked on failure with the failing result.</param>
        public void Match(Action<T> onSuccess, Action<Result<T>> onFailure)
        {
            if (result.IsSuccess) onSuccess(result.Value!);
            else onFailure(result);
        }

        /// <summary>
        /// Pattern-match dispatch that returns a value. Useful to map a
        /// <see cref="Result{T}"/> into another type and return directly from
        /// callers without requiring the null-forgiving operator on the value.
        /// </summary>
        /// <typeparam name="TDest">Type of the value returned by both branches.</typeparam>
        /// <param name="onSuccess">Function invoked on success with the carried value.</param>
        /// <param name="onFailure">Function invoked on failure with the failing result.</param>
        /// <returns>The value produced by the branch that ran.</returns>
        public TDest Match<TDest>(Func<T, TDest> onSuccess, Func<Result<T>, TDest> onFailure) =>
            result.IsSuccess
                ? onSuccess(result.Value!)
                : onFailure(result);

        /// <summary>
        /// Pattern-match dispatch that runs side effects and returns the original
        /// result for further chaining (tap-style).
        /// </summary>
        /// <param name="onSuccess">Action invoked on success with the carried value.</param>
        /// <param name="onFailure">Action invoked on failure with the failing result.</param>
        /// <returns>The original result, unchanged.</returns>
        public Result<T> MatchAndPropagate(Action<T> onSuccess, Action<Result<T>> onFailure)
        {
            if (result.IsSuccess)
            {
                onSuccess(result.Value!);
                return result;
            }
            else
            {
                onFailure(result);
                return result;
            }
        }

        /// <summary>
        /// Asynchronous pattern-match dispatch that returns a value.
        /// </summary>
        /// <typeparam name="TDest">Type of the value returned by both branches.</typeparam>
        /// <param name="onSuccess">Async function invoked on success with the carried value.</param>
        /// <param name="onFailure">Async function invoked on failure with the failing result.</param>
        /// <returns>A task containing the value produced by the branch that ran.</returns>
        public async Task<TDest> MatchAsync<TDest>(Func<T, Task<TDest>> onSuccess, Func<Result<T>, Task<TDest>> onFailure) =>
            result.IsSuccess
                ? await onSuccess(result.Value!)
                : await onFailure(result);
    }

    extension<T>(Task<Result<T>> task)
    {
        /// <summary>
        /// Await the source task and pattern-match the awaited <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="onSuccess">Action invoked on success with the carried value.</param>
        /// <param name="onFailure">Action invoked on failure with the failing result.</param>
        /// <returns>A task that completes once the matched action returns.</returns>
        public async Task MatchAsync(Action<T> onSuccess, Action<Result<T>> onFailure)
        {
            var result = await task;
            if (result.IsSuccess) onSuccess(result.Value!);
            else onFailure(result);
        }

        /// <summary>
        /// Await the source task and pattern-match with async branches that return a value.
        /// </summary>
        /// <typeparam name="TDest">Type of the value returned by both branches.</typeparam>
        /// <param name="onSuccess">Async function invoked on success with the carried value.</param>
        /// <param name="onFailure">Async function invoked on failure with the failing result.</param>
        /// <returns>A task containing the value produced by the branch that ran.</returns>
        public async Task<TDest> MatchAsync<TDest>(Func<T, Task<TDest>> onSuccess, Func<Result<T>, Task<TDest>> onFailure)
        {
            var result = await task;
            return result.IsSuccess
                ? await onSuccess(result.Value!)
                : await onFailure(result);
        }

        /// <summary>
        /// Await the source task and pattern-match with synchronous branches that return a value.
        /// </summary>
        /// <typeparam name="TDest">Type of the value returned by both branches.</typeparam>
        /// <param name="onSuccess">Function invoked on success with the carried value.</param>
        /// <param name="onFailure">Function invoked on failure with the failing result.</param>
        /// <returns>A task containing the value produced by the branch that ran.</returns>
        public async Task<TDest> MatchAsync<TDest>(Func<T, TDest> onSuccess, Func<Result<T>, TDest> onFailure)
        {
            var result = await task;
            return result.IsSuccess
                ? onSuccess(result.Value!)
                : onFailure(result);
        }
    }
}
