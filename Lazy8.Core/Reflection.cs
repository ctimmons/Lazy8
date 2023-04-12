/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Reflection;
namespace Lazy8.Core;

public static class ReflectionUtils
{
  /* Code for both GetPropValue methods is from StackOverflow answer https://stackoverflow.com/a/1197004/116198
     posted by user Ed S. (https://stackoverflow.com/users/1053/ed-s).

     Modifications:

       I made the non-generic GetPropValue method private.
       Added an 'else' clause to one of the 'if' statements.

     Both GetPropValue methods, and my modifications to those methods, are licensed
     under CC BY-SA 3.0 (https://creativecommons.org/licenses/by-sa/3.0/)
     See https://stackoverflow.com/help/licensing for more info. */

  private static Object GetPropValue(Object obj, String name)
  {
    foreach (var part in name.Split('.'))
    {
      if (obj == null)
        return null;

      Type type = obj.GetType();
      PropertyInfo info = type.GetProperty(part);
      if (info == null)
        return null;

      obj = info.GetValue(obj, null);
    }

    return obj;
  }

  /// <summary>
  /// Generic method to get an object's property value.
  /// </summary>
  /// <typeparam name="T">The property's type.</typeparam>
  /// <param name="obj">A System.Object.</param>
  /// <param name="name">The property's name.</param>
  /// <returns></returns>
  public static T GetPropValue<T>(this Object obj, String name)
  {
    Object retval = GetPropValue(obj, name);
    if (retval == null)
      return default;
    else
      /* Throws an InvalidCastException if types are incompatible. */
      return (T) retval;
  }

  /// <summary>
  /// Copy the data from the IEnumerable sources into the DataTable target.
  /// <para>
  /// One way to understand this method is to visualize the 'target' pulling data from 'sources'.
  /// If there's a converter available for the target column, the converter pulls and modifies
  /// the source data before inserting it into the target.Otherwise a simple 1-1 mapping is done.
  /// </para>
  /// </summary>
  /// <typeparam name="Source"></typeparam>
  /// <param name="sources"></param>
  /// <param name="target"></param>
  /// <param name="converters"></param>
  public static void PopulateDataTable<Source>(IEnumerable<Source> sources, DataTable target, Dictionary<String, Action<Source, DataRow>> converters = null)
  {
    void addRowToTarget(Source source)
    {
      /* GetProperties() returns a PropertyInfo[].  An array is only searchable by its numeric
         index.  However, this code needs to search the array by the property's name.
         Create a dictionary to allow searching by the property name. */
      var sourcePropertyInfos = typeof(Source).GetProperties().ToImmutableDictionary(pi => pi.Name, pi => pi);
      var targetRow = target.NewRow();

      foreach (DataColumn targetColumn in target.Columns)
      {
        if ((converters != null) && converters.TryGetValue(targetColumn.ColumnName, out var converter))
          converter(source, targetRow);
        else
          targetRow[targetColumn.ColumnName] = sourcePropertyInfos[targetColumn.ColumnName].GetValue(source) ?? DBNull.Value;
      }

      target.Rows.Add(targetRow);
    }

    foreach (var source in sources)
      addRowToTarget(source);
  }

  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="Source"></typeparam>
  /// <typeparam name="Target"></typeparam>
  /// <param name="source"></param>
  /// <param name="converters"></param>
  /// <returns></returns>
  public static Target GetTargetFromSource<Source, Target>(Source source, Dictionary<String, Action<Source, Target>> converters = null)
    where Source : class
    where Target : class, new()
  {
    var target = new Target();

    /* GetProperties() returns a PropertyInfo[].  An array is only searchable by its numeric
       index.  However, this code needs to search the array by the property's name.
       Create a dictionary to allow searching by the property name. */
    var sourcePropertyInfos = typeof(Source).GetProperties().ToImmutableDictionary(pi => pi.Name, pi => pi);

    foreach (var targetPropertyInfo in typeof(Target).GetProperties())
    {
      if ((converters != null) && converters.TryGetValue(targetPropertyInfo.Name, out var converter))
        converter(source, target);
      else
        targetPropertyInfo.SetValue(target, sourcePropertyInfos[targetPropertyInfo.Name].GetValue(source));
    }

    return target;
  }

  /// <summary>
  /// Return an <see cref="IEnumerable{String}"/> containing all of the public instance field
  /// names of type <typeparamref name="T"/>.
  /// </summary>
  /// <typeparam name="T">Any type.</typeparam>
  /// <returns>An <see cref="IEnumerable{String}"/>.</returns>
  public static IEnumerable<String> GetPublicFieldNames<T>() =>
    typeof(T)
    .GetFields(BindingFlags.Public | BindingFlags.Instance)
    .Select(pi => pi.Name);

  /// <summary>
  /// Return an <see cref="IEnumerable{String}"/> containing all of the public instance property
  /// names of type <typeparamref name="T"/>.
  /// </summary>
  /// <typeparam name="T">Any type.</typeparam>
  /// <returns>An <see cref="IEnumerable{String}"/>.</returns>
  public static IEnumerable<String> GetPublicPropertyNames<T>() =>
    typeof(T)
    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
    .Select(pi => pi.Name);

  /// <summary>
  /// Return a <see cref="String"/> containing a list of all public property names and their associated values for object <paramref name="source"/>.
  /// </summary>
  /// <typeparam name="T">Any type.</typeparam>
  /// <param name="source">Any <see cref="Object"/>.</param>
  /// <returns>A <see cref="String"/>.</returns>
  public static String GetPublicPropertyValues<T>(Object source) where T : class =>
    GetPublicPropertyNames<T>()
    .Select(propertyName => new { Name = propertyName, Value = GetPropValue<T>(source, propertyName) })
    .Select(propertyNameValuePair => propertyNameValuePair.Name + " = " + ((propertyNameValuePair.Value == null) ? "NULL" : propertyNameValuePair.Value))
    .Join("\n");

  /// <summary>
  /// Return a <see cref="String"/> containing a list of all property names and their associated values for object <paramref name="source"/>,
  /// where the properties match the supplied <paramref name="bindingFlags"/>.
  /// </summary>
  /// <param name="source">Any <see cref="Object"/>.</param>
  /// <param name="bindingFlags">One or more <see cref="BindingFlags"/> values (may be OR-ed together).</param>
  /// <returns>A <see cref="String"/>.</returns>
  public static String GetPropertyValues(Object source, BindingFlags bindingFlags)
  {
    var type = source.GetType();
    return
      type
      .GetProperties(bindingFlags)
      .Select(propertyInfo => propertyInfo.Name + " = " + (type.GetProperty(propertyInfo.Name).GetValue(source, null) ?? "NULL"))
      .Join("\n");
  }

  /// <summary>
  /// Given an object <paramref name="instance"/>, return a <see cref="String"/> containing the construction expression used to
  /// build the instance.  Useful in T4 templates.
  /// <para>
  /// The returned string contains only those property settings that differ from the instance's default property values.
  /// </para>
  /// </summary>
  /// <typeparam name="T">Any class that has a parameterless constructor.</typeparam>
  /// <param name="instance">Any <see cref="Object"/>.</param>
  /// <param name="propertiesToIgnore">Zero or more <see cref="String"/>s, each containing a property name to ignore.</param>
  /// <returns></returns>
  public static String GetObjectInitializer<T>(T instance, params String[] propertiesToIgnore)
    where T : class, new()
  {
    var result = new List<String>();

    var t = typeof(T);
    var defaultInstance = new T();
    var publicInstanceProperties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var propertyInfo in publicInstanceProperties)
    {
      if (propertyInfo.GetIndexParameters().Any())
        continue;

      if (propertiesToIgnore.ContainsCI(propertyInfo.Name))
        continue;

      var defaultInstanceValue = propertyInfo.GetValue(defaultInstance);
      var newInstanceValue = propertyInfo.GetValue(instance);
      if (!Equals(defaultInstanceValue, newInstanceValue))
        result.Add($"{propertyInfo.Name} = {GetLiteralDisplayValue(newInstanceValue)}");
    }

    return $"new {t.Name}() {{ {result.OrderBy(s => s).Join(", ")} }}";
  }

  private static String GetLiteralDisplayValue(Object value)
  {
    value.Name(nameof(value)).NotNull();

    if (value is Char)
    {
      return $"'{value}'";
    }
    else if (value is String)
    {
      return $"\"{value}\"";
    }
    else if (value is Enum)
    {
      var ve = (value as Enum);
      var typename = ve.GetType().Name;
      return
        ve
        .ToString()
        .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        .Select(v => $"{typename}.{v.Trim()}")
        .OrderBy(s => s)
        .Join(" | ");
    }
    else
    {
      return value.ToString();
    }
  }
}

