public class Timer
{
    private CancellationTokenSource? _cts;

    public void Start(int milliseconds, string displayName, ITransportClient client, ChatClientFSM fsm)
    {
        Cancel();
        _cts = new CancellationTokenSource();
        Debugger.PrintStatus($"Starting timer for {milliseconds} milliseconds...");
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(milliseconds, _cts.Token);
                Debugger.PrintError("No reply from server. Exiting...");
                string errMsg = ClientMessageBuilder.BuildError(displayName, "No reply from server. Exiting...");
                await client.SendAsync(errMsg);
                CancellationSource.Cancel();
                fsm._state = ChatClientFSM.ClientState.end;
                await fsm.EndStateAsync();
                Environment.Exit(1);
            }
            catch (TaskCanceledException)
            {
                Debugger.PrintStatus("Timer was cancelled before timeout.");
            }
        });
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
