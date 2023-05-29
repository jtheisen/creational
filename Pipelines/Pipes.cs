using ICSharpCode.SharpZipLib.BZip2;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Xml;
using System.Xml.Serialization;

namespace Pipelines;

public interface IPipeEnd<B>
{
    void Run(B nextBuffer, IPipeContext context);
}

public interface IWorker
{
    public String Name { get; }
}

public interface IStreamPipeEnd : IPipeEnd<Pipe> { }

public interface IEnumerablePipeEnd<T> : IPipeEnd<BlockingCollection<T>> { }

public class FilePipeEnd : IStreamPipeEnd, IWorker
{
    private readonly FileInfo fileInfo;

    public String Name => "file";

    public FilePipeEnd(String fileName)
    {
        fileInfo = new FileInfo(fileName);
    }

    public void Run(Pipe pipe, IPipeContext context)
    {
        context.AddWorker(this);

        switch (context.Mode)
        {
            case PipeRunMode.Suck:
                context.SetTask("reading", Suck(pipe));

                break;
            case PipeRunMode.Blow:
                context.SetTask("writing", Blow(pipe));

                break;
            default:
                break;
        }
    }

    async Task Suck(Pipe pipe)
    {
        using var stream = fileInfo.OpenRead();

        await stream.CopyToAsync(pipe.Writer);
    }

    async Task Blow(Pipe pipe)
    {
        using var stream = fileInfo.OpenWrite();

        await pipe.Reader.CopyToAsync(stream);
    }
}

public class ZipPipeEnd : IStreamPipeEnd, IWorker
{
    private readonly IStreamPipeEnd nestedPipeEnd;

    public ZipPipeEnd(IStreamPipeEnd sourcePipeEnd)
    {
        this.nestedPipeEnd = sourcePipeEnd;
    }

    public String Name => "zip";

    public void Run(Pipe nextPipe, IPipeContext context)
    {
        var pipe = Buffers.MakeBuffer<Pipe>();

        nestedPipeEnd.Run(pipe, context);

        context.AddBuffer(pipe);

        context.AddWorker(this);

        switch (context.Mode)
        {
            case PipeRunMode.Probe:
                break;
            case PipeRunMode.Suck:
                {
                    var stream = new BZip2InputStream(pipe.Reader.AsStream());

                    context.SetTask("decrompressing", stream.CopyToAsync(nextPipe.Writer));
                }
                break;
            case PipeRunMode.Blow:
                {
                    var stream = new BZip2OutputStream(pipe.Writer.AsStream());

                    context.SetTask("compressing", nextPipe.Reader.CopyToAsync(stream));
                }
                break;
            default:
                break;
        }
    }
}

public class ParserPipeEnd<T> : IEnumerablePipeEnd<T>, IWorker
{
    private readonly IStreamPipeEnd sourcePipeEnd;

    XmlSerializer serializer = new XmlSerializer(typeof(T));

    public ParserPipeEnd(IStreamPipeEnd sourcePipeEnd)
    {
        this.sourcePipeEnd = sourcePipeEnd;
    }

    public String Name => "xml";

    public void Run(BlockingCollection<T> buffer, IPipeContext context)
    {
        var pipe = Buffers.MakeBuffer<Pipe>();

        sourcePipeEnd.Run(pipe, context);

        context.AddBuffer(pipe);

        context.AddWorker(this);

        switch (context.Mode)
        {
            case PipeRunMode.Suck:
                context.Schedule("parsing", () => Parse(pipe.Reader, buffer));

                break;
            case PipeRunMode.Blow:
                throw new NotImplementedException();

            default:
                break;
        }
    }

    void Parse(PipeReader pipeReader, BlockingCollection<T> sink)
    {
        var reader = XmlReader.Create(pipeReader.AsStream());

        reader.MoveToContent();

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    if (reader.LocalName == "page")
                    {
                        var page = (T)serializer.Deserialize(reader);

                        sink.Add(page);
                    }
                    break;
            }
        }

        sink.CompleteAdding();
    }
}

public class QueryablePipeEnd<T> : IEnumerablePipeEnd<T>, IWorker
{
    private readonly IQueryable<T> source;

    public QueryablePipeEnd(IQueryable<T> source)
    {
        this.source = source;
    }

    public String Name => "queryable";

    public void Run(BlockingCollection<T> nextBuffer, IPipeContext context)
    {
        context.AddWorker(this);

        switch (context.Mode)
        {
            case PipeRunMode.Suck:
                context.Schedule("ingesting", progress =>
                {
                    var length = source.LongCount();

                    progress.ReportTotal(length);

                    var processed = 0L;

                    foreach (var item in source)
                    {
                        nextBuffer.Add(item);

                        progress.ReportProcessed(++processed);
                    }

                    nextBuffer.CompleteAdding();
                });
                break;
            case PipeRunMode.Blow:
                throw new NotImplementedException($"Blowing into a {nameof(QueryablePipeEnd<T>)} is not supported");
            default:
                break;
        }
    }
}

public class ActionPipeEnd<T> : IEnumerablePipeEnd<T>, IWorker
{
    private readonly Action<T> action;

    public ActionPipeEnd(Action<T> action)
    {
        this.action = action;
    }

    public String Name => "action";

    public void Run(BlockingCollection<T> nextBuffer, IPipeContext context)
    {
        context.AddWorker(this);

        switch (context.Mode)
        {
            case PipeRunMode.Suck:
                throw new NotImplementedException($"Sucking from a {nameof(ActionPipeEnd<T>)} is not supported");
            case PipeRunMode.Blow:
                context.Schedule("calling", () =>
                {
                    foreach (var item in nextBuffer.GetConsumingEnumerable())
                    {
                        action(item);
                    }
                });
                break;
            default:
                break;
        }
    }
}

public class AsyncActionPipeEnd<T> : IEnumerablePipeEnd<T>, IWorker
{
    private readonly Func<T, Task> action;

    public AsyncActionPipeEnd(Func<T, Task> action)
    {
        this.action = action;
    }

    public String Name => "action";

    public void Run(BlockingCollection<T> nextBuffer, IPipeContext context)
    {
        context.AddWorker(this);

        switch (context.Mode)
        {
            case PipeRunMode.Suck:
                throw new NotImplementedException($"Sucking from a {nameof(ActionPipeEnd<T>)} is not supported");
            case PipeRunMode.Blow:
                context.Schedule("calling", async () =>
                {
                    foreach (var item in nextBuffer.GetConsumingEnumerable())
                    {
                        await action(item);
                    }
                });
                break;
            default:
                break;
        }
    }
}

public class EnumerableTransformEnumerablePipeEnd<S, T> : IEnumerablePipeEnd<T>, IWorker
{
    private readonly IEnumerablePipeEnd<S> nestedPipeEnd;
    private readonly Func<IEnumerable<S>, IEnumerable<T>> map;

    public EnumerableTransformEnumerablePipeEnd(IEnumerablePipeEnd<S> nestedPipeEnd, Func<IEnumerable<S>, IEnumerable<T>> map)
    {
        this.nestedPipeEnd = nestedPipeEnd;
        this.map = map;
    }

    public String Name => "transform";

    public void Run(BlockingCollection<T> nextBuffer, IPipeContext context)
    {
        var buffer = Buffers.MakeBuffer<BlockingCollection<S>>();

        nestedPipeEnd.Run(buffer, context);

        context.AddBuffer(buffer);

        context.AddWorker(this);

        switch (context.Mode)
        {
            case PipeRunMode.Suck:
                context.Schedule("transforming", () => Transform(buffer, nextBuffer));
                break;
            case PipeRunMode.Blow:
                throw new Exception($"This worker does not support blowing");
            default:
                break;
        }
    }

    void Transform(BlockingCollection<S> buffer, BlockingCollection<T> sink)
    {
        var items = map(buffer.GetConsumingEnumerable());

        foreach (var item in items)
        {
            sink.Add(item);
        }

        sink.CompleteAdding();
    }
}

public class AsyncEnumerableTransformEnumerablePipeEnd<S, T> : IEnumerablePipeEnd<T>, IWorker
{
    private readonly IEnumerablePipeEnd<S> nestedPipeEnd;
    private readonly Func<IEnumerable<S>, IAsyncEnumerable<T>> map;

    public AsyncEnumerableTransformEnumerablePipeEnd(IEnumerablePipeEnd<S> nestedPipeEnd, Func<IEnumerable<S>, IAsyncEnumerable<T>> map)
    {
        this.nestedPipeEnd = nestedPipeEnd;
        this.map = map;
    }

    public String Name => "transform";

    public void Run(BlockingCollection<T> nextBuffer, IPipeContext context)
    {
        var buffer = Buffers.MakeBuffer<BlockingCollection<S>>();

        nestedPipeEnd.Run(buffer, context);

        context.AddBuffer(buffer);

        context.AddWorker(this);

        switch (context.Mode)
        {
            case PipeRunMode.Suck:
                context.ScheduleAsync("transforming", () => Transform(buffer, nextBuffer));
                break;
            case PipeRunMode.Blow:
                throw new Exception($"This worker does not support blowing");
            default:
                break;
        }
    }

    async Task Transform(BlockingCollection<S> buffer, BlockingCollection<T> sink)
    {
        var items = map(buffer.GetConsumingEnumerable());

        await foreach (var item in items)
        {
            sink.Add(item);
        }

        sink.CompleteAdding();
    }
}

public class TransformEnumerablePipeEnd<S, T> : IEnumerablePipeEnd<T>, IWorker
{
    private readonly IEnumerablePipeEnd<S> nestedPipeEnd;
    private readonly Func<S, T> map;
    private readonly Func<T, S> reverseMap;

    public TransformEnumerablePipeEnd(IEnumerablePipeEnd<S> nestedPipeEnd, Func<S, T> map, Func<T, S> reverseMap = null)
    {
        this.nestedPipeEnd = nestedPipeEnd;
        this.map = map;
        this.reverseMap = reverseMap;
    }

    public String Name => "transform";

    public void Run(BlockingCollection<T> nextBuffer, IPipeContext context)
    {
        var buffer = Buffers.MakeBuffer<BlockingCollection<S>>();

        nestedPipeEnd.Run(buffer, context);

        context.AddBuffer(buffer);

        context.AddWorker(this);

        switch (context.Mode)
        {
            case PipeRunMode.Suck:
                context.Schedule("transforming", () => Transform(buffer, nextBuffer, map));
                break;
            case PipeRunMode.Blow:
                context.Schedule("transforming", () => Transform(nextBuffer, buffer, reverseMap));
                break;
            default:
                break;
        }
    }

    void Transform<S2, T2>(BlockingCollection<S2> buffer, BlockingCollection<T2> sink, Func<S2, T2> map)
    {
        while (!buffer.IsCompleted)
        {
            var item = buffer.Take();

            sink.Add(map(item));
        }

        sink.CompleteAdding();
    }
}

public class AsyncTransformEnumerablePipeEnd<S, T> : IEnumerablePipeEnd<T>, IWorker
{
    private readonly IEnumerablePipeEnd<S> nestedPipeEnd;
    private readonly Func<S, Task<T>> map;
    private readonly Func<T, Task<S>> reverseMap;

    public AsyncTransformEnumerablePipeEnd(IEnumerablePipeEnd<S> nestedPipeEnd, Func<S, Task<T>> map, Func<T, Task<S>> reverseMap = null)
    {
        this.nestedPipeEnd = nestedPipeEnd;
        this.map = map;
        this.reverseMap = reverseMap;
    }

    public String Name => "transform";

    public void Run(BlockingCollection<T> nextBuffer, IPipeContext context)
    {
        var buffer = Buffers.MakeBuffer<BlockingCollection<S>>();

        nestedPipeEnd.Run(buffer, context);

        context.AddBuffer(buffer);

        context.AddWorker(this);

        switch (context.Mode)
        {
            case PipeRunMode.Suck:
                context.ScheduleAsync("transforming", () => Transform(buffer, nextBuffer, map));
                break;
            case PipeRunMode.Blow:
                context.ScheduleAsync("transforming", () => Transform(nextBuffer, buffer, reverseMap));
                break;
            default:
                break;
        }
    }

    async Task Transform<S2, T2>(BlockingCollection<S2> buffer, BlockingCollection<T2> sink, Func<S2, Task<T2>> map)
    {
        while (!buffer.IsCompleted)
        {
            var item = buffer.Take();

            var transformed = await map(item);

            sink.Add(transformed);
        }

        sink.CompleteAdding();
    }
}

public static class Pipes
{
    public static IStreamPipeEnd File(String fileName) => new FilePipeEnd(fileName);

    public static IStreamPipeEnd Zip(this IStreamPipeEnd source) => new ZipPipeEnd(source);

    public static IEnumerablePipeEnd<T> FromQueryable<T>(this IQueryable<T> source)
        => new QueryablePipeEnd<T>(source);

    public static IEnumerablePipeEnd<T> FromAction<T>(Action<T> source)
        => new ActionPipeEnd<T>(source);

    public static IEnumerablePipeEnd<T> FromAsyncAction<T>(Func<T, Task> source)
        => new AsyncActionPipeEnd<T>(source);

    public static IEnumerablePipeEnd<T> ParseXml<T>(this IStreamPipeEnd source) => new ParserPipeEnd<T>(source);

    public static IEnumerablePipeEnd<T> Transform<S, T>(this IEnumerablePipeEnd<S> source, Func<IEnumerable<S>, IEnumerable<T>> map)
        => new EnumerableTransformEnumerablePipeEnd<S, T>(source, map);

    public static IEnumerablePipeEnd<T> Map<S, T>(this IEnumerablePipeEnd<S> source, Func<S, T> map, Func<T, S> reverseMap = null)
        => new TransformEnumerablePipeEnd<S, T>(source, map, reverseMap);

    public static IEnumerablePipeEnd<T> Map<S, T>(this IEnumerablePipeEnd<S> source, Func<S, Task<T>> map, Func<T, Task<S>> reverseMap = null)
        => new AsyncTransformEnumerablePipeEnd<S, T>(source, map, reverseMap);

    public static IEnumerablePipeEnd<T> Do<T>(this IEnumerablePipeEnd<T> source, Action<T> action)
    {
        Func<T, T> map = t =>
        {
            action(t);

            return t;
        };

        return new TransformEnumerablePipeEnd<T, T>(source, map, map);
    }

    public static IPipeline BuildCopyingPipeline<B>(this IPipeEnd<B> source, IPipeEnd<B> sink)
        where B : class, new()
    {
        return new Pipeline<B>(source, sink);
    }

    public static LivePipeline Start(this IPipeline pipeline)
    {
        pipeline.Run(out var livePipeline);
        
        return livePipeline;
    }

    public static void Wait(this LivePipeline pipeline) => pipeline.Task.Wait();

    public static Task WaitAsync(this LivePipeline pipeline) => pipeline.Task;
}
