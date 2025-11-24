using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuickCode.Cli.Models;

namespace QuickCode.Cli.Services;

public sealed class QuickCodeApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _verbose;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public QuickCodeApiClient(string baseUrl, bool verbose = false, HttpMessageHandler? handler = null)
    {
        _verbose = verbose;
        _httpClient = handler is null
            ? new HttpClient()
            : new HttpClient(handler);

        _httpClient.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public void Dispose() => _httpClient.Dispose();

    public Task<bool> CreateProjectAsync(string projectName, string projectEmail) =>
        PostAsync<bool>("api/GenerateSite/CreateProject", new
        {
            projectName,
            projectEmail
        });

    public Task<bool> CheckProjectNameAsync(string projectName) =>
        GetAsync<bool>($"api/Dbml/check-project-name/{projectName}");

    public Task<bool> CheckSecretCodeAsync(string projectName, string email, string secret) =>
        GetAsync<bool>($"api/Dbml/check-secret-code/{projectName}/{email}/{secret}");

    public Task<bool> ForgotSecretCodeAsync(string projectName, string email) =>
        PostAsync<bool>("api/GenerateSite/ForgotProjectSecret", new
        {
            projectName,
            projectEmail = email
        });

    public Task<JsonElement> GetAvailableModulesAsync() =>
        GetAsync<JsonElement>("api/Dbml/get-modules");

    public Task<JsonElement> GetProjectModulesAsync(string projectName) =>
        GetAsync<JsonElement>($"api/Dbml/get-project-modules/{projectName}");

    public Task<string> GetModuleDbmlAsync(string projectName, string moduleName, string templateKey) =>
        GetStringAsync($"api/Dbml/get-module-dbml/{projectName}/{moduleName}/{templateKey}");

    public Task<bool> AddProjectModuleAsync(
        string projectName,
        string email,
        string secret,
        string moduleName,
        string moduleTemplateKey,
        string dbTypeKey,
        string architecturalPatternKey) =>
        PostAsync<bool>("api/Dbml/add-project-module", new
        {
            projectName,
            projectEmail = email,
            projectSecretCode = secret,
            moduleName,
            moduleTemplateKey,
            dbTypeKey,
            architecturalPatternKey
        });

    public Task<bool> RemoveProjectModuleAsync(
        string projectName,
        string email,
        string secret,
        string moduleName) =>
        PostAsync<bool>("api/Dbml/remove-project-module", new
        {
            projectName,
            projectEmail = email,
            projectSecretCode = secret,
            moduleName
        });

    public Task<bool> SaveModuleDbmlAsync(
        string projectName,
        string email,
        string secret,
        string moduleName,
        string moduleTemplateKey,
        string dbml,
        string dbTypeKey) =>
        PostAsync<bool>("api/Dbml/save-module-dbml", new
        {
            projectName,
            projectEmail = email,
            projectSecretCode = secret,
            moduleName,
            moduleTemplateKey,
            dbml,
            dbTypeKey
        });

    public Task<bool> GenerateProjectSolutionAsync(
        string projectName,
        string email,
        string secret,
        string sessionId) =>
        PostAsync<bool>("api/GenerateSite/GenerateProjectSolution", new
        {
            projectName,
            projectEmail = email,
            projectSecretCode = secret,
            sessionId
        });

    public Task<ActiveProjectResponse?> GetActiveProjectAsync(string sessionId) =>
        GetAsync<ActiveProjectResponse?>($"api/GenerateSite/GetActiveProjectBySessionId?sessionId={sessionId}");

    public Task<JsonElement> GetGenerationStepsAsync() =>
        GetAsync<JsonElement>("api/GenerateSite/GetGenerationSteps");

    private async Task<T> GetAsync<T>(string relativeUrl)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        return await SendAsync<T>(request);
    }

    private async Task<string> GetStringAsync(string relativeUrl)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        var response = await SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<T> PostAsync<T>(string relativeUrl, object payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json")
        };
        return await SendAsync<T>(request);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        string? requestBody = null;
        if (request.Content is not null)
        {
            requestBody = await request.Content.ReadAsStringAsync();
            request.Content = new StringContent(requestBody,
                Encoding.UTF8,
                request.Content.Headers.ContentType?.MediaType ?? "application/json");
        }

        if (_verbose)
        {
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"REQUEST: {request.Method} {request.RequestUri}");
            if (requestBody is not null)
            {
                Console.WriteLine(requestBody);
            }
            Console.WriteLine(new string('=', 80));
        }

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (_verbose)
        {
            Console.WriteLine($"RESPONSE {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine(responseBody);
            Console.WriteLine(new string('=', 80));
        }

        if (!response.IsSuccessStatusCode)
        {
            PrintHttpError(request, requestBody, response, responseBody);
            var message = ExtractErrorMessage(responseBody, (int)response.StatusCode);
            throw new InvalidOperationException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {message}");
        }

        response.Content = new StringContent(responseBody,
            Encoding.UTF8,
            response.Content.Headers.ContentType?.MediaType ?? "application/json");
        return response;
    }

    private static void PrintHttpError(HttpRequestMessage request, string? requestBody, HttpResponseMessage response, string responseBody)
    {
        Console.WriteLine(new string('-', 80));
        Console.WriteLine("HTTP request failed:");
        Console.WriteLine($"{request.Method} {request.RequestUri}");
        if (!string.IsNullOrWhiteSpace(requestBody))
        {
            Console.WriteLine("Request Body:");
            Console.WriteLine(requestBody);
        }

        Console.WriteLine($"Response {(int)response.StatusCode} {response.StatusCode}");
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            Console.WriteLine("Response Body:");
            Console.WriteLine(responseBody);
        }
        Console.WriteLine(new string('-', 80));
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request)
    {
        var response = await SendAsync(request);
        if (response.Content.Headers.ContentLength == 0)
        {
            return default!;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions);
        if (result is null)
        {
            throw new InvalidOperationException("Unexpected empty response from API.");
        }

        return result;
    }

    private static string ExtractErrorMessage(string? content, int statusCode)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"API error ({statusCode}).";
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                return doc.RootElement.GetString() ?? $"API error ({statusCode}).";
            }

            if (doc.RootElement.TryGetProperty("message", out var messageProp))
            {
                return messageProp.GetString() ?? $"API error ({statusCode}).";
            }

            if (doc.RootElement.TryGetProperty("error", out var errorProp))
            {
                return errorProp.GetString() ?? $"API error ({statusCode}).";
            }
        }
        catch
        {
            // ignored
        }

        return content.Length > 500 ? content[..500] + "..." : content;
    }
}

