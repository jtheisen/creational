using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Web;

public static class Extensions
{
    public static T Modify<T>(this T source, Action<T> action)
    {
        action(source);

        return source;
    }

    public static S Apply<S>(this S source, Action<S> func)
    {
        func(source);

        return source;
    }

    public static T Apply<S, T>(this S source, Func<S, T> func)
        => func(source);

    public static Task<T> Ensure<T>(this Task<T> task)
        => task ?? Task.FromResult<T>(default);

    public static T Apply<T>(this T source, Boolean indeed, Func<T, T> func)
        => indeed ? func(source) : source;

    public static T Apply<S, T>(this S s, Func<S, T> fun, Action<S> onException)
    {
        var haveReachedEnd = false;
        try
        {
            var temp = fun(s);
            haveReachedEnd = true;
            return temp;
        }
        finally
        {
            if (!haveReachedEnd)
            {
                onException(s);
            }
        }
    }

    public static String BreakIf(Boolean flag)
    {
        if (flag)
        {
            Debugger.Break();
        }

        return "";
    }

    public static T Assert<T>(this T value, Predicate<T> predicate, String message = null)
    {
        if (!predicate(value))
        {
            throw new Exception(message);
        }

        return value;
    }

    public static T AssertWeakly<T>(this T value, Predicate<T> predicate, String message = null)
    {
        if (!predicate(value))
        {
            LogManager.GetLogger("Assertions").Error(message);

            Debugger.Break();
        }

        return value;
    }

    public static T AssertNotNull<T>(this T value, String message)
        where T : class
    {
        if (value == null)
        {
            throw new Exception(message);
        }

        return value;
    }

    public static V AddOrUpdateSafer<K, V>(this ConcurrentDictionary<K, V> dict, K key, Func<V> factory, Action<V> update)
    {
        return dict.AddOrUpdate(key, _ => factory().Apply(update), (_, v) => v.Apply(update));
    }

    public static void Ignore(this Task _) { }

    public static string GetDescription(this Enum value)
    {
        Type type = value.GetType();
        string name = Enum.GetName(type, value);
        if (name != null)
        {
            var field = type.GetField(name);
            if (field != null)
            {
                var attr = Attribute.GetCustomAttribute(
                    field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute
                ;

                if (attr != null) return attr.Description;
            }
        }
        return null;
    }

    public static (T[] t, T[] f) ToArrays<T>(this (IEnumerable<T> t, IEnumerable<T> f) source)
        => (source.t.ToArray(), source.f.ToArray());

    public static (IEnumerable<T> t, IEnumerable<T> f) GroupByBoolean<T>(this IEnumerable<T> source, Func<T, Boolean> groupBy)
    {
        var groups = source.GroupBy(groupBy);

        var f = groups.FirstOrDefault(g => !g.Key);
        var t = groups.FirstOrDefault(g => g.Key);

        return (t ?? Enumerable.Empty<T>(), f ?? Enumerable.Empty<T>());
    }

    public static IEnumerable<(T1, T2)> FullOuterJoin<T1, T2, K>(this IEnumerable<T1> s1, IEnumerable<T2> s2, Func<T1, K> k1, Func<T2, K> k2)
    {
        var l1 = s1.ToLookup(i => k1(i), i => i);
        var l2 = s2.ToLookup(i => k2(i), i => i);

        foreach (var i in s1)
        {
            var r2 = l2[k1(i)];

            var hadItems = false;

            foreach (var j in r2)
            {
                yield return (i, j);

                hadItems = true;
            }

            if (!hadItems)
            {
                yield return (i, default);
            }
        }

        foreach (var j in s2)
        {
            if (l1[k2(j)].Any()) continue;

            yield return (default, j);
        }
    }

    public static NameValueCollection GetQueryFromUrl(this String url)
    {
        var uri = new Uri(url);
        return HttpUtility.ParseQueryString(uri.Query);
    }

    public static Uri WithChangedQuery(this Uri uri, Action<NameValueCollection> change)
    {
        var builder = new UriBuilder(uri);
        var query = HttpUtility.ParseQueryString(builder.Query);
        change(query);
        builder.Query = query.ToString();
        return builder.Uri;
    }

    public static void Time(Action func, out TimeSpan elapsed)
    {
        var watch = new Stopwatch();

        watch.Start();

        try
        {
            func();
        }
        finally
        {
            watch.Stop();

            elapsed = watch.Elapsed;
        }
    }

    public static S Time<S>(Func<S> func, out TimeSpan elapsed)
    {
        var watch = new Stopwatch();

        watch.Start();

        try
        {
            return func();
        }
        finally
        {
            watch.Stop();

            elapsed = watch.Elapsed;
        }
    }

    public static T Single<T>(this IEnumerable<T> source, String message)
    {
        try
        {
            return source.Single();
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(message, ex);
        }
    }

    public static String[] SplitIntoNonemptyLines(this String multiline)
        => multiline.Split('\n').Select(l => l.Trim()).Where(l => !String.IsNullOrWhiteSpace(l)).ToArray();

    public static Boolean NextBoolean(this Random random)
        => random.Next(2) == 0;

    public static Double NextDouble(this Random random, Double min, Double max)
        => random.NextDouble() * (max - min) + min;

    //public static IServiceCollection Clone(this IServiceCollection service)
    //{
    //    var result = new ServiceCollection();

    //    var collection = result as ICollection<ServiceDescriptor>;

    //    foreach (var item in service)
    //    {
    //        collection.Add(item);
    //    }

    //    return result;
    //}

    public static String ToJson(this Double d)
    {
        return d.ToString(CultureInfo.InvariantCulture);
    }
}
