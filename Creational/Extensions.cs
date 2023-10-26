using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Creational;

public static class Extensions
{
    public static Step AsFailedStep(this Step step) => (Step)(-(Int32)step.AsNormalStep());

    public static Step AsNormalStep(this Step step) => (Step)Math.Abs((Int32)step);

    public static DataTable ToDataTable<T>(this IEnumerable<T> source)
        where T : class
    {
        var firstItem = source?.FirstOrDefault();

        if (firstItem is null) throw new Exception("source is null or empty");

        static Boolean CheckPropertyType(PropertyInfo property)
        {
            var type = property.PropertyType;
            if (type == typeof(String)) return true;
            if (type.IsValueType) return true;
            return false;
        }

        var table = new DataTable();
        var props = firstItem.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(CheckPropertyType)
            .ToArray()
            ;
        table.Columns.AddRange(props
            .Select(field => new DataColumn(field.Name, field.PropertyType))
            .ToArray()
        );
        var fieldCount = props.Length;

        foreach (var item in source)
        {
            table.Rows.Add(Enumerable.Range(0, fieldCount)
                .Select(index => props[index].GetValue(item)
                ).ToArray()
            );
        }

        return table;
    }

    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
        => source is null ? Enumerable.Empty<T>() : source;

    public static (Int32 total, Int32 pending, Int32 inError) LoadWorkingStats(this ApplicationDb db, Step step)
    {
        var errorStep = step.AsFailedStep();

        return (
            db.Pages.Where(p => p.Type == PageType.Content).Count(),
            db.Pages.Where(p => p.Type == PageType.Content && p.Step == step).Count(),
            db.Pages.Where(p => p.Type == PageType.Content && p.Step == errorStep).Count()
        );
    }

    public class EncodingTruncator
    {
        Encoder encoder;
        Byte[] buffer;
        Int32 ellipsisBytes;
        String ellipsis;

        public Int32 MaxBytes => buffer.Length;

        public EncodingTruncator(Encoding encoding, Int32 bufferSize, String ellipsis = "...")
        {
            encoder = encoding.GetEncoder();
            ellipsisBytes = encoder.GetByteCount(ellipsis.AsSpan(), true);
            this.ellipsis = ellipsis;
            bufferSize = Math.Max(bufferSize, ellipsisBytes);
            bufferSize = Math.Max(bufferSize, 1);
            buffer = new Byte[bufferSize];
        }

        public String Truncate(String s, Int32 maxBytes) => Truncate(s, maxBytes, out _, out _, out _);

        public String Truncate(String s, Int32 maxBytes, out Int32 charsUsed, out Int32 bytesUsed, out Boolean untruncated)
        {
            if (maxBytes > buffer.Length)
            {
                throw new Exception($"Buffer size of {buffer.Length} is too small for max byte length of {maxBytes}");
            }
            else if (maxBytes < ellipsisBytes)
            {
                throw new Exception($"Max byte length of {maxBytes} isn't even enough for the ellipsis '{ellipsis}'");
            }

            var sSpan = s.AsSpan();
            var bSpan = buffer.AsSpan();

            encoder.Convert(sSpan, bSpan[..maxBytes], true, out charsUsed, out bytesUsed, out untruncated);

            if (untruncated)
            {
                return s;
            }
            else
            {
                var remainingBytes = maxBytes - bytesUsed;

                while (remainingBytes < ellipsisBytes)
                {
                    encoder.Convert(sSpan[(charsUsed - 1)..charsUsed], bSpan, true, out _, out var characterByteSize, out _);

                    remainingBytes += characterByteSize;
                    --charsUsed;
                }

                return s.Substring(0, charsUsed) + ellipsis;
            }
        }
    }

    [ThreadStatic]
    static EncodingTruncator utf8Truncator;

    public static String TruncateUtf8(this String s, Int32 maxBytes)
    {
        if (maxBytes < 1) throw new Exception("maxBytes must be at least 1");

        if (utf8Truncator is null || maxBytes > utf8Truncator.MaxBytes)
        {
            utf8Truncator = new EncodingTruncator(Encoding.UTF8, maxBytes);
        }

        return utf8Truncator.Truncate(s, maxBytes);
    }
}
