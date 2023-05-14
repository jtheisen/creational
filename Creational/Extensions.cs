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
    public static Step AsFailedStep(this Step step) => (Step)(-(Int32)step);

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
}
