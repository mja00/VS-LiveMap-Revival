using livemap.util;

namespace livemap.task;

public class AsyncTaskManager {
    private readonly List<AsyncTask> _tasks = [];

    public AsyncTaskManager(LiveMap server) {
        _tasks.Add(new MarkersTask(server));
        _tasks.Add(new SettingsTask(server));
    }

    public void Tick() {
        foreach (AsyncTask task in _tasks) {
            try {
                task.Tick();
            } catch (Exception e) {
                Logger.Error(e.ToString());
            }
        }
    }

    public void Dispose() {
        foreach (AsyncTask task in _tasks) {
            try {
                task.Dispose();
            } catch (Exception e) {
                Logger.Error(e.ToString());
            }
        }
    }
}
