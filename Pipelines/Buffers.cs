using System;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Reflection;

namespace Pipelines;

public interface IBufferHandler
{
    PipeReportBufferPart GetReport(Object buffer);

    Object MakeBuffer();
}

public abstract class AbstractBufferHandler<B> : IBufferHandler
{
    public PipeReportBufferPart GetReport(Object buffer) => GetReportImpl((B)buffer);

    public abstract PipeReportBufferPart GetReportImpl(B buffer);

    public Object MakeBuffer() => MakeBufferImpl();

    public abstract B MakeBufferImpl();
}

public class BlockingCollectionBufferHandler<T> : AbstractBufferHandler<BlockingCollection<T>>
{
    public override PipeReportBufferPart GetReportImpl(BlockingCollection<T> buffer) => buffer.GetReport();

    public override BlockingCollection<T> MakeBufferImpl() => new BlockingCollection<T>(1024);
}

public class PipeBufferHandler : AbstractBufferHandler<Pipe>
{
    public override PipeReportBufferPart GetReportImpl(Pipe buffer) => buffer.GetReport();

    public override Pipe MakeBufferImpl() => new Pipe();
}

public static class BufferHandlers
{
    static ConcurrentDictionary<Type, IBufferHandler> handlers;

    static BufferHandlers()
    {
        handlers = new ConcurrentDictionary<Type, IBufferHandler>();
    }

    public static IBufferHandler GetHandler(Type bufferType)
        => handlers.GetOrAdd(bufferType, ChooseHandler);

    static IBufferHandler ChooseHandler(Type type)
    {
        if (type == typeof(Pipe))
        {
            return new PipeBufferHandler();
        }
        else if (type.IsGenericType)
        {
            var td = type.GetGenericTypeDefinition();

            if (td == typeof(BlockingCollection<>))
            {
                var tp = type.GetGenericArguments();

                var handlerType = typeof(BlockingCollectionBufferHandler<>).MakeGenericType(tp);

                return (IBufferHandler)Activator.CreateInstance(handlerType);
            }
        }

        throw new Exception($"No known handler for buffer type {type}");
    }
}

public static class Buffers
{
    static PropertyInfo pipeLengthProperty = typeof(Pipe).GetProperty("Length", BindingFlags.NonPublic | BindingFlags.Instance);
    static PropertyInfo resumeWriterThresholdProperty = typeof(Pipe).GetProperty("ResumeWriterThreshold", BindingFlags.NonPublic | BindingFlags.Instance);
    static PropertyInfo pauseWriterThresholdProperty = typeof(Pipe).GetProperty("PauseWriterThreshold", BindingFlags.NonPublic | BindingFlags.Instance);

    public static B MakeBuffer<B>()
    {
        var bufferType = typeof(B);

        var handler = BufferHandlers.GetHandler(bufferType);

        return (B)handler.MakeBuffer();
    }

    public static PipeReportPart GetAppropriateReport(Object buffer)
    {
        var bufferType = buffer.GetType();

        var handler = BufferHandlers.GetHandler(bufferType);

        return handler.GetReport(buffer);
    }

    public static PipeReportBufferPart GetReport<T>(this BlockingCollection<T> buffer)
        => new PipeReportBufferPart(
            GetBufferState(buffer.Count, buffer.BoundedCapacity >> 2, buffer.BoundedCapacity >> 1),
            buffer.Count,
            buffer.BoundedCapacity
        );

    public static PipeReportBufferPart GetReport(this Pipe buffer)
    {
        var count = (Int64)pipeLengthProperty.GetValue(buffer);
        var resumeThreshold = (Int64)resumeWriterThresholdProperty.GetValue(buffer);
        var pauseThreshold = (Int64)pauseWriterThresholdProperty.GetValue(buffer);

        return new PipeReportBufferPart(
            GetBufferState(count, resumeThreshold >> 2, resumeThreshold >> 1),
            count,
            pauseThreshold
        );
    }

    public static PipeReportBufferState GetBufferState(Int64 count, Int64 lower, Int64 upper)
    {
        if (count < lower) return PipeReportBufferState.Empty;
        if (count > upper) return PipeReportBufferState.Full;
        return PipeReportBufferState.Mixed;
    }
}
