using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace QuickCode.Cli.Services;

public sealed class GenerationWatcher : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly bool _verbose;

    public event Action<GenerationUpdateEvent>? OnUpdate;

    public GenerationWatcher(string apiBaseUrl, string sessionId, bool verbose = false)
    {
        _verbose = verbose;
        var baseUri = apiBaseUrl.EndsWith('/') ? apiBaseUrl[..^1] : apiBaseUrl;
        var hubUrl = $"{baseUri}/quickcodeHub?sessionId={sessionId}";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<JsonElement, JsonElement, JsonElement, JsonElement>("UpdateGeneratorStatus",
            (projectId, actionId, allActions, allSteps) =>
            {
                OnUpdate?.Invoke(new GenerationUpdateEvent(projectId, actionId, allActions, allSteps));
            });

        _connection.Closed += error =>
        {
            if (_verbose)
            {
                Console.WriteLine(error is null
                    ? "SignalR connection closed."
                    : $"SignalR closed: {error.Message}");
            }
            return Task.CompletedTask;
        };
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await _connection.StartAsync(cancellationToken);
        if (_verbose)
        {
            Console.WriteLine("✅ SignalR connected");
        }

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _connection.StopAsync();
        }
        catch
        {
            // ignore
        }
        await _connection.DisposeAsync();
    }
}

public sealed record GenerationUpdateEvent(
    JsonElement ProjectId,
    JsonElement ActionId,
    JsonElement AllActions,
    JsonElement AllSteps);

