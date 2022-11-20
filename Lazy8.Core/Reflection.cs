﻿/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lazy8.Core;

public class ReflectionUtils
{
  public static T GetPropertyValue<T>(Object obj, String propName) where T : class
  {
    obj.Name(nameof(obj)).NotNull();
    propName.Name(nameof(propName)).NotNullEmptyOrOnlyWhitespace();

    var pi = obj.GetType().GetProperty(propName);
    if (pi == null)
      throw new Exception(String.Format(Properties.Resources.Reflection_PropertyNotFound, propName));

    return (T) pi.GetValue(obj, null);
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
    .Select(propertyName => new { Name = propertyName, Value = GetPropertyValue<T>(source, propertyName) })
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

