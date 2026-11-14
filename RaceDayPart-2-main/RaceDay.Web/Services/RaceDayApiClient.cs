using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace RaceDay.Web.Services;

public sealed record ApiResult<T>(bool Succeeded, T? Value, string? Error = null, HttpStatusCode? StatusCode = null);

public sealed class RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiResult<T>> GetAsync<T>(string route, CancellationToken token = default) =>
        SendAsync<T>(HttpMethod.Get, route, null, token);

    public Task<ApiResult<T>> GetAsync<T>(string route, IReadOnlyDictionary<string, string?> query, CancellationToken token = default) =>
        SendAsync<T>(HttpMethod.Get, QueryHelpers.AddQueryString(route, query.Where(x => x.Value is not null).ToDictionary(x => x.Key, x => x.Value)), null, token);

    public Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string route, TRequest body, CancellationToken token = default) =>
        SendAsync<TResponse>(HttpMethod.Post, route, body, token);

    public Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(string route, TRequest body, CancellationToken token = default) =>
        SendAsync<TResponse>(HttpMethod.Put, route, body, token);

    public Task<ApiResult<TResponse>> DeleteAsync<TResponse>(string route, CancellationToken token = default) =>
        SendAsync<TResponse>(HttpMethod.Delete, route, null, token);

    public Task<ApiResult<TResponse>> UploadFileAsync<TResponse>(string route, IFormFile file, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, route);
        var multipart = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        if (System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(file.ContentType, out var contentType))
            fileContent.Headers.ContentType = contentType;
        multipart.Add(fileContent, "file", Path.GetFileName(file.FileName));
        request.Content = multipart;
        return SendRequestAsync<TResponse>(request, token);
    }

    private Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string route, object? body, CancellationToken token)
    {
        var request = new HttpRequestMessage(method, route);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        return SendRequestAsync<T>(request, token);
    }

    private async Task<ApiResult<T>> SendRequestAsync<T>(HttpRequestMessage request, CancellationToken token)
    {
        using (request)
        {
            var context = accessor.HttpContext;
            request.RequestUri = new Uri(http.BaseAddress!, request.RequestUri!);
            var apiCookie = context?.Request.Cookies[".RaceDay.Session"];
            if (!string.IsNullOrEmpty(apiCookie))
                request.Headers.Add("Cookie", $".RaceDay.Session={apiCookie}");

            try
            {
                using var response = await http.SendAsync(request, token);
                if (response.Headers.TryGetValues("Set-Cookie", out var cookies) && context is not null)
                    foreach (var cookie in cookies)
                        context.Response.Headers.Append("Set-Cookie", cookie);

                if (!response.IsSuccessStatusCode)
                {
                    var error = response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized => "Your session has expired or the supplied credentials are invalid.",
                        HttpStatusCode.Forbidden => "You do not have permission to complete that action.",
                        HttpStatusCode.NotFound => "The requested RaceDay information could not be found.",
                        HttpStatusCode.Conflict => "That change conflicts with existing RaceDay information.",
                        HttpStatusCode.BadRequest => "Please review the information and try again.",
                        _ => "RaceDay could not complete your request right now."
                    };
                    return new(false, default, error, response.StatusCode);
                }

                if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
                    return new(true, default, null, response.StatusCode);

                var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, token);
                return new(true, value, null, response.StatusCode);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                return new(false, default, "RaceDay is taking too long to respond. Please try again.");
            }
            catch (HttpRequestException)
            {
                return new(false, default, "RaceDay is temporarily unavailable. Please try again later.");
            }
            catch (JsonException)
            {
                return new(false, default, "RaceDay returned an unexpected response. Please try again later.");
            }
        }
    }
}
