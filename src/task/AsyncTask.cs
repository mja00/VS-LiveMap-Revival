namespace livemap.task;

public abstract class AsyncTask(LiveMap server) {
    protected readonly LiveMap _server = server;
    private readonly CancellationTokenSource _cts = new();

    private volatile bool _running;

    public async void Tick() {
        try {
            if (_running) {
                return;
            }

            _running = true;
            await TickAsync(_cts.Token);
        } catch (Exception e) {
            await Console.Error.WriteLineAsync(e.ToString());
        } finally {
            _running = false;
        }
    }

    protected abstract Task TickAsync(CancellationToken cancellationToken);

    public virtual void Dispose() {
        _cts.Cancel();
    }
}
