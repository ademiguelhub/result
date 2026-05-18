namespace FluentRecordResults.Extensions.AspNetCore.Http;

/// <summary>
/// Base class for typed service clients that communicate via HTTP and return <see cref="Result{T}"/> or <see cref="Result"/>.
/// Provides standard methods for GET/POST/PUT/PATCH/DELETE operations with automatic Result deserialization.
/// </summary>
public abstract class ServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Override to supply custom JSON serializer options for both request serialization and response deserialization.
    /// </summary>
    protected virtual JsonSerializerOptions? JsonSerializerOptions => null;

    /// <summary>Sends a GET request and deserializes the response as <see cref="Result{TResponse}"/>.</summary>
    protected Task<Result<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(ct => httpClient.GetAsync(path, ct), cancellationToken);

    /// <summary>Sends a GET request and deserializes the response as <see cref="Result"/>.</summary>
    protected Task<Result> GetAsync(string path, CancellationToken cancellationToken = default) =>
        SendAsync(ct => httpClient.GetAsync(path, ct), cancellationToken);

    /// <summary>Sends a POST request with a JSON body and deserializes the response as <see cref="Result{TResponse}"/>.</summary>
    protected Task<Result<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(ct => httpClient.PostAsJsonAsync(path, body, JsonSerializerOptions, ct), cancellationToken);

    /// <summary>Sends a POST request with a JSON body and deserializes the response as <see cref="Result"/>.</summary>
    protected Task<Result> PostAsync<TRequest>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync(ct => httpClient.PostAsJsonAsync(path, body, JsonSerializerOptions, ct), cancellationToken);

    /// <summary>Sends a PUT request with a JSON body and deserializes the response as <see cref="Result{TResponse}"/>.</summary>
    protected Task<Result<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(ct => httpClient.PutAsJsonAsync(path, body, JsonSerializerOptions, ct), cancellationToken);

    /// <summary>Sends a PUT request with a JSON body and deserializes the response as <see cref="Result"/>.</summary>
    protected Task<Result> PutAsync<TRequest>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync(ct => httpClient.PutAsJsonAsync(path, body, JsonSerializerOptions, ct), cancellationToken);

    /// <summary>Sends a PATCH request with a JSON body and deserializes the response as <see cref="Result{TResponse}"/>.</summary>
    protected Task<Result<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(ct => httpClient.PatchAsJsonAsync(path, body, JsonSerializerOptions, ct), cancellationToken);

    /// <summary>Sends a PATCH request with a JSON body and deserializes the response as <see cref="Result"/>.</summary>
    protected Task<Result> PatchAsync<TRequest>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync(ct => httpClient.PatchAsJsonAsync(path, body, JsonSerializerOptions, ct), cancellationToken);

    /// <summary>Sends a DELETE request and deserializes the response as <see cref="Result{TResponse}"/>.</summary>
    protected Task<Result<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(ct => httpClient.DeleteAsync(path, ct), cancellationToken);

    /// <summary>Sends a DELETE request and deserializes the response as <see cref="Result"/>.</summary>
    protected Task<Result> DeleteAsync(string path, CancellationToken cancellationToken = default) =>
        SendAsync(ct => httpClient.DeleteAsync(path, ct), cancellationToken);

    private async Task<Result<T>> SendAsync<T>(Func<CancellationToken, Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        try
        {
            var response = await send(cancellationToken).ConfigureAwait(false);
            return await DeserializeResultAsync<T>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return Result<T>.Fail(Error.Timeout, $"Request timed out: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            return Result<T>.Fail(Error.Cancelled, $"Request was cancelled: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            return Result<T>.Fail(Error.Error, $"HTTP request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(Error.SerializationError, $"Unexpected error: {ex.Message}");
        }
    }

    private async Task<Result> SendAsync(Func<CancellationToken, Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        try
        {
            var response = await send(cancellationToken).ConfigureAwait(false);
            return await DeserializeResultAsync(response, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return Result.Fail(Error.Timeout, $"Request timed out: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            return Result.Fail(Error.Cancelled, $"Request was cancelled: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            return Result.Fail(Error.Error, $"HTTP request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail(Error.SerializationError, $"Unexpected error: {ex.Message}");
        }
    }

    private async Task<Result<T>> DeserializeResultAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            if (IsEmptyResponse(response))
            {
                var (code, message) = GetResponseErrorDetails(response, string.Empty);
                return Result<T>.Fail(code, message);
            }

            var result = await response.Content
                .ReadFromJsonAsync<Result<T>>(JsonSerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return result ?? Result<T>.Fail(
                Error.SerializationError,
                $"Empty response body (HTTP {(int)response.StatusCode})");
        }
        catch (JsonException)
        {
            var contentBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var (code, message) = GetResponseErrorDetails(response, contentBody);
            return Result<T>.Fail(code, message);
        }
    }

    private async Task<Result> DeserializeResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            if (IsEmptyResponse(response))
            {
                var (code, message) = GetResponseErrorDetails(response, string.Empty);
                return Result.Fail(code, message);
            }

            var result = await response.Content
                .ReadFromJsonAsync<Result>(JsonSerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return result ?? Result.Fail(
                Error.SerializationError,
                $"Empty response body (HTTP {(int)response.StatusCode})");
        }
        catch (JsonException)
        {
            var contentBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var (code, message) = GetResponseErrorDetails(response, contentBody);
            return Result.Fail(code, message);
        }
    }

    private static bool IsEmptyResponse(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.NoContent ||
        response.Content.Headers.ContentLength == 0;

    private static (Error, string) GetResponseErrorDetails(HttpResponseMessage response, string contentBody)
    {
        var prefix = $"[path={response.RequestMessage?.RequestUri}] HTTP {(int)response.StatusCode}: ";
        return string.IsNullOrWhiteSpace(contentBody)
            ? (Error.Error, prefix + response.ReasonPhrase)
            : (Error.SerializationError, prefix + "Invalid JSON response");
    }
}
