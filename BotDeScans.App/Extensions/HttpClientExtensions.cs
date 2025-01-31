using FluentResults;
using System.Text.Json;
namespace BotDeScans.App.Extensions;

public static class HttpClientExtensions
{
    public static async Task<Result<T>> TryGetContent<T>(
        this HttpResponseMessage httpResponseMessage,
        JsonSerializerOptions jsonSerializerOptions,
        CancellationToken cancellationToken)
    {
        var stringContent = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            var errorMessage = $"Unsuccessful response to login, statuscode: {httpResponseMessage.StatusCode}. " +
                               $"Attempted url: {httpResponseMessage.RequestMessage!.RequestUri!.AbsoluteUri}. " +
                                "More details in inner error.";

            var error = new Error(errorMessage);
            var innerError = new Error(stringContent);
            error.Reasons.Add(innerError);

            return Result.Fail(error);
        }

        if (typeof(T) == typeof(string))
            return Result.Ok((T)Convert.ChangeType(stringContent, typeof(string)));

        var data = JsonSerializer.Deserialize<T>(stringContent, jsonSerializerOptions);
        return data is null
            ? Result.Fail<T>($"Request returned an empty object response. Raw response: {stringContent}")
            : Result.Ok(data);
    }

    public static void NormalizeBoundary(this MultipartFormDataContent content)
    {
        var boundaryValue = content.Headers.ContentType!.Parameters.FirstOrDefault(p => p.Name == "boundary");
        if (boundaryValue != null)
            boundaryValue.Value = boundaryValue.Value!.Replace("\"", string.Empty);
    }
}
