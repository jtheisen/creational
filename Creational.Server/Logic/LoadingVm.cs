namespace Creational;

public class LoadingVm<T>
{
    private readonly Func<CancellationToken, Task<T>> loader;
    private readonly Action notify;

    CancellationTokenSource currentCts;

    Task<T> currentLoadTask;

    public T Value { get; set; }

    public Exception Exception { get; set; }

    public Boolean HadValue { get; set; }

    public Boolean IsLoading { get; set; }

    public LoadingVm(Func<CancellationToken, Task<T>> loader, Action notify = null, T initialValue = default)
    {
        this.loader = loader;
        this.notify = notify;

        Value = initialValue;
    }

    public LoadingVm(Func<Task<T>> loader, Action notify = null, T initialValue = default)
    {
        this.loader = _ => loader();
        this.notify = notify;

        Value = initialValue;
    }

    public async Task Load()
    {
        if (currentLoadTask?.IsCompleted == false)
        {
            currentCts.Cancel();
        }

        currentCts = new CancellationTokenSource();

        var task = currentLoadTask = loader(currentCts.Token);

        try
        {
            await currentLoadTask;

            if (currentLoadTask != task) return;

            Value = currentLoadTask.Result;
            Exception = null;
            HadValue = true;
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        notify?.Invoke();
    }
}
