using livemap.util;

namespace livemap.task;

public abstract class AsyncTask(LiveMap server) {
    private readonly CancellationTokenSource _cts = new();
    protected readonly LiveMap _server = server;

    private volatile bool _running;

    public async void Tick() {
        try {
            if (_running) {
                return;
            }

            _running = true;
            await TickAsync(_cts.Token);
        } catch (Exception e) {
            Logger.Error(e.ToString());
        } finally {
            _running = false;
        }
    }

    protected abstract Task TickAsync(CancellationToken cancellationToken);

    public virtual void Dispose() => _cts.Cancel();
}
