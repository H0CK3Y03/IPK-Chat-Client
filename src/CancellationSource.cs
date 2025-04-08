// singleton
public class CancellationSource
{
    private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

    public static CancellationToken Token => _cts.Token;

    public static void Cancel()
    {
        if (!_cts.IsCancellationRequested)
            _cts.Cancel();
    }

    public static bool IsCancellationRequested => _cts.IsCancellationRequested;
}