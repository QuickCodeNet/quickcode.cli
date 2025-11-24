using QuickCode.Cli.Models;

namespace QuickCode.Cli.Services;

public sealed class GenerationPollingService
{
    private readonly QuickCodeApiClient _client;

    public GenerationPollingService(QuickCodeApiClient client)
    {
        _client = client;
    }

    public async Task RunAsync(string sessionId, TimeSpan interval, Func<ActiveProjectResponse, Task> onUpdate, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var response = await _client.GetActiveProjectAsync(sessionId);
            if (response is not null)
            {
                await onUpdate(response);
                if (response.IsFinished)
                {
                    return;
                }
            }

            try
            {
                await Task.Delay(interval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

