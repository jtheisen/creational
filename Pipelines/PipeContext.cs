namespace Pipelines;

public class PipePart { }

public class PipeBufferPart : PipePart
{
    public Object Buffer { get; init; }
}

public class PipeWorkerPart : PipePart
{
    public IWorker Worker { get; init; }
    public String Verb { get; set; }
    public WorkerInputProgress Progress { get; set; }
    public Task Task { get; set; }
}

public enum PipeRunMode
{
    Probe,
    Suck,
    Blow
}

public interface IWorkerInputProgress
{
    void ReportTotal(Int64 total);
    void ReportProcessed(Int64 processed);
}

public class WorkerInputProgress : IWorkerInputProgress
{
    Int64 total;
    Int64 processed;

    public void ReportTotal(Int64 total) => this.total = total;
    public void ReportProcessed(Int64 processed) => this.processed = processed;

    public Int64 Total => this.total;
    public Int64 Processed => this.processed;
}

public interface IPipeContext
{
    PipeRunMode Mode { get; }

    void AddBuffer(Object buffer);

    void AddWorker<W>(W worker) where W : IWorker;

    void SetTask(String verb, Task task);

    void Schedule(String verb, Action<IWorkerInputProgress> task);

    void Schedule(String verb, Action task);

    void ScheduleAsync(String verb, Func<Task> task);

    void ScheduleAsync(String verb, Func<IWorkerInputProgress, Task> task);
}

public class PipeContext : IPipeContext
{
    PipeRunMode mode;
    List<PipePart> parts;

    public PipeRunMode Mode => mode;

    public PipeContext(PipeRunMode mode)
    {
        this.mode = mode;
        this.parts = new List<PipePart>();
    }

    public IEnumerable<PipePart> Parts => parts;

    public void AddBuffer(Object buffer) => parts.Add(new PipeBufferPart { Buffer = buffer });

    public void AddWorker<W>(W worker) where W : IWorker => parts.Add(new PipeWorkerPart { Worker = worker });

    public void SetTask(String verb, Task task)
    {
        SetTask(verb, _ => task);
    }

    void SetTask(String verb, Func<IWorkerInputProgress, Task> getTask)
    {
        var lastPart = parts.LastOrDefault() as PipeWorkerPart;

        if (lastPart is null) throw new Exception($"Can't set task without a worker part");

        var progress = new WorkerInputProgress();

        lastPart.Task = getTask(progress);
        lastPart.Verb = verb;
        lastPart.Progress = progress;
    }



    public void Schedule(String verb, Action task) => SetTask(verb, _ => Task.Run(task));
    public void Schedule(String verb, Action<IWorkerInputProgress> task) => SetTask(verb, p => Task.Run(() => task(p)));

    public void ScheduleAsync(String verb, Func<Task> task) => SetTask(verb, _ => task());
    public void ScheduleAsync(String verb, Func<IWorkerInputProgress, Task> task) => SetTask(verb, task);
}

public interface IPipeline
{
    void Run(out LivePipeline livePipeline);
}

public class Pipeline<B> : IPipeline
    where B : class, new()
{
    private readonly IPipeEnd<B> source;
    private readonly IPipeEnd<B> sink;

    public Pipeline(IPipeEnd<B> source, IPipeEnd<B> sink)
    {
        this.source = source;
        this.sink = sink;
    }

    public void Run(out LivePipeline livePipeline)
    {
        livePipeline = Instantiate();
    }

    LivePipeline Instantiate()
    {
        var buffer = new B();

        var suckingContext = new PipeContext(PipeRunMode.Suck);
        var blowingContext = new PipeContext(PipeRunMode.Blow);

        source.Run(buffer, suckingContext);
        sink.Run(buffer, blowingContext);

        var parts = suckingContext.Parts
            .Concat(new[] { new PipeBufferPart { Buffer = buffer } })
            .Concat(blowingContext.Parts.Reverse());

        return new LivePipeline(parts);
    }
}

public class LivePipeline
{
    private PipePart[] parts;

    Task task;

    public Task Task => task;

    public LivePipeline(IEnumerable<PipePart> parts)
    {
        this.parts = parts.ToArray();

        var tasks = from p in parts.OfType<PipeWorkerPart>() let t = p.Task where t is not null select t;

        task = Task.WhenAll(tasks);
    }

    public PipeReport GetReport()
    {
        var parts = this.parts.Select(GetReportPart).ToArray();

        return new PipeReport(parts);
    }

    static PipeReportPart GetReportPart(PipePart part) => part switch
    {
        PipeBufferPart bufferPart => Buffers.GetAppropriateReport(bufferPart.Buffer),
        PipeWorkerPart workerPart => new PipeReportWorker(workerPart.Worker.Name, workerPart.Progress, GetState(workerPart.Task)),
        _ => null
    };

    static PipeReportWorkerState GetState(Task task)
    {
        if (task is null) return PipeReportWorkerState.Ready;

        if (task.IsCanceled) return PipeReportWorkerState.Cancelled;

        if (task.IsCompletedSuccessfully) return PipeReportWorkerState.Completed;

        if (task.IsFaulted) return PipeReportWorkerState.Failed;

        return PipeReportWorkerState.Running;
    }
}

public record PipeReportPart;

public enum PipeReportBufferState
{
    Empty,
    Mixed,
    Full
}

public record PipeReportBufferPart(PipeReportBufferState State, Int64 Content, Int64 Size) : PipeReportPart;

public enum PipeReportWorkerState
{
    Ready,
    Running,
    Completed,
    Cancelled,
    Failed
}

public record PipeReportWorker(String Name, WorkerInputProgress Progress, PipeReportWorkerState State) : PipeReportPart;

public record PipeReport(PipeReportPart[] Parts);

