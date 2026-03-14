namespace Infrastructure.Contracts.Flows.Responses;

/// <summary>
/// Internal envelope returned by flow use cases.
/// Pattern: Success = true → Data populated. Success = false → ErrorMessage populated.
/// </summary>
public sealed record FlowResult<T>(
    bool Success,
    T? Data = default,
    string? ErrorMessage = null,
    int StatusCode = 200
)
{
    public static FlowResult<T> Ok(T data) => new(true, Data: data);

    public static FlowResult<T> Fail(string message, int statusCode = 400) =>
        new(false, ErrorMessage: message, StatusCode: statusCode);

    public static FlowResult<T> NotFound(string message = "Not found.") =>
        new(false, ErrorMessage: message, StatusCode: 404);
}
