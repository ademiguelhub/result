namespace FluentRecordResults.Extensions;

/// <summary>
/// Extensions to chain operations that return a <see cref="Result"/> or
/// <see cref="Result{T}"/>. On failure, the failure is propagated; on success,
/// the next operation is invoked with the carried value (flatMap / monadic bind).
/// Sync and async overloads are provided for both <see cref="Result{T}"/> and
/// <see cref="Task{TResult}"/>-wrapped results.
/// </summary>
public static class ResultBindExtensions
{
    extension<T>(Result<T> result)
    {
        /// <summary>
        /// Chain an operation that returns a non-generic <see cref="Result"/>.
        /// </summary>
        /// <param name="onSuccess">Invoked with the value when this result is successful.</param>
        /// <returns>The result of <paramref name="onSuccess"/> on success; otherwise a failed <see cref="Result"/> propagating the current failure.</returns>
        public Result Bind(Func<T, Result> onSuccess) =>
            result.IsSuccess ? onSuccess(result.Value!) : Result.Fail(result.Code, result.Reason);

        /// <summary>
        /// Chain an operation that returns a <see cref="Result{TDest}"/>.
        /// </summary>
        /// <typeparam name="TDest">Type of the value carried by the bound result.</typeparam>
        /// <param name="onSuccess">Invoked with the value when this result is successful.</param>
        /// <returns>The result of <paramref name="onSuccess"/> on success; otherwise a failed <see cref="Result{TDest}"/> propagating the current failure.</returns>
        public Result<TDest> Bind<TDest>(Func<T, Result<TDest>> onSuccess) =>
            result.IsSuccess ? onSuccess(result.Value!) : Result<TDest>.Fail(result.Code, result.Reason);

        /// <summary>
        /// Asynchronously chain an operation that returns a non-generic <see cref="Result"/>.
        /// </summary>
        /// <param name="onSuccess">Async function invoked with the value when this result is successful.</param>
        /// <returns>A task containing the bound <see cref="Result"/>.</returns>
        public async Task<Result> BindAsync(Func<T, Task<Result>> onSuccess) =>
            result.IsSuccess ? await onSuccess(result.Value!) : Result.Fail(result.Code, result.Reason);

        /// <summary>
        /// Asynchronously chain an operation that returns a <see cref="Result{TDest}"/>.
        /// </summary>
        /// <typeparam name="TDest">Type of the value carried by the bound result.</typeparam>
        /// <param name="onSuccess">Async function invoked with the value when this result is successful.</param>
        /// <returns>A task containing the bound <see cref="Result{TDest}"/>.</returns>
        public async Task<Result<TDest>> BindAsync<TDest>(Func<T, Task<Result<TDest>>> onSuccess) =>
            result.IsSuccess ? await onSuccess(result.Value!) : Result<TDest>.Fail(result.Code, result.Reason);
    }

    extension<T>(Task<Result<T>> task)
    {
        /// <summary>
        /// Await the source task and chain a synchronous operation that returns a non-generic <see cref="Result"/>.
        /// </summary>
        /// <param name="onSuccess">Invoked with the value when the awaited result is successful.</param>
        /// <returns>A task containing the bound <see cref="Result"/>.</returns>
        public async Task<Result> BindAsync(Func<T, Result> onSuccess)
        {
            var result = await task;
            return result.IsSuccess ? onSuccess(result.Value!) : Result.Fail(result.Code, result.Reason);
        }

        /// <summary>
        /// Await the source task and chain an asynchronous operation that returns a non-generic <see cref="Result"/>.
        /// </summary>
        /// <param name="onSuccess">Async function invoked with the value when the awaited result is successful.</param>
        /// <returns>A task containing the bound <see cref="Result"/>.</returns>
        public async Task<Result> BindAsync(Func<T, Task<Result>> onSuccess)
        {
            var result = await task;
            return result.IsSuccess ? await onSuccess(result.Value!) : Result.Fail(result.Code, result.Reason);
        }

        /// <summary>
        /// Await the source task and chain a synchronous operation that returns a <see cref="Result{TDest}"/>.
        /// </summary>
        /// <typeparam name="TDest">Type of the value carried by the bound result.</typeparam>
        /// <param name="onSuccess">Invoked with the value when the awaited result is successful.</param>
        /// <returns>A task containing the bound <see cref="Result{TDest}"/>.</returns>
        public async Task<Result<TDest>> BindAsync<TDest>(Func<T, Result<TDest>> onSuccess)
        {
            var result = await task;
            return result.IsSuccess ? onSuccess(result.Value!) : Result<TDest>.Fail(result.Code, result.Reason);
        }

        /// <summary>
        /// Await the source task and chain an asynchronous operation that returns a <see cref="Result{TDest}"/>.
        /// </summary>
        /// <typeparam name="TDest">Type of the value carried by the bound result.</typeparam>
        /// <param name="onSuccess">Async function invoked with the value when the awaited result is successful.</param>
        /// <returns>A task containing the bound <see cref="Result{TDest}"/>.</returns>
        public async Task<Result<TDest>> BindAsync<TDest>(Func<T, Task<Result<TDest>>> onSuccess)
        {
            var result = await task;
            return result.IsSuccess ? await onSuccess(result.Value!) : Result<TDest>.Fail(result.Code, result.Reason);
        }
    }
}
