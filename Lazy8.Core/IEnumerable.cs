/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Lazy8.Core
{
  public enum UseOxfordComma { No, Yes }

  public static class IEnumerableUtils
  {
    /// <summary>
    /// A case-insensitive version of the <see cref="Enumerable.Contains"/> method for use on <see cref="IEnumerable&lt;String&gt;"/> values.
    /// <para>Returns a <see cref="Boolean"/> indicating if <paramref name="searchValue"/> is contained in <paramref name="values"/>.</para>
    /// </summary>
    /// <param name="values">An <see cref="IEnumerable&lt;String&gt;"/> value.</param>
    /// <param name="searchValue">A <see cref="String"/> value (cannot be null or have a zero length).</param>
    /// <returns></returns>
    public static Boolean ContainsCI(this IEnumerable<String> values, String searchValue) => values.Any(s => s.EqualsCI(searchValue));

    /// <summary>
    /// Concatenate all of the characters in <paramref name="values"/> into a <see cref="String"/>.
    /// </summary>
    /// <param name="values">An <see cref="IEnumerable&lt;Char&gt;"/> value.</param>
    /// <returns>A <see cref="String"/> containing all of the characters in <paramref name="values"/> concatenated together.</returns>
    public static String Join(this IEnumerable<Char> values) => String.Join("", values);

    /// <summary>
    /// This is the fluent version of the <see cref="String.Join"/> method.
    /// <para>See the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.string.join?view=netcore-3.1#System_String_Join_System_String_System_Collections_Generic_IEnumerable_System_String__">MSDN entry.</a></para>
    /// </summary>
    /// <param name="values">A collection that contains the strings to concatenate.</param>
    /// <param name="separator">The string to use as a separator. <paramref name="separator"/> is included in the returned string only if values has more than one element.</param>
    /// <returns>A string that consists of the members of values delimited by the separator string.
    /// <para>-or-</para>
    /// <para>Empty if values has zero elements or all the elements of values are null.</para>
    /// </returns>
    public static String Join(this IEnumerable<String> values, String separator) => String.Join(separator, values);

    /// <summary>
    /// Concatenate the values in an <see cref="IEnumerable&lt;String&gt;"/>, placing an empty string between each value.
    /// </summary>
    /// <param name="values">A collection that contains the strings to concatenate.</param>
    /// <returns>A string that consists of the members of values delimited by the separator string.
    /// <para>-or-</para>
    /// <para>Empty if values has zero elements or all the elements of values are null.</para>
    /// </returns>
    public static String Join(this IEnumerable<String> values) => values.Join("");

    private static String JoinAndOr(IEnumerable<String> values, String connector, UseOxfordComma useOxfordComma = UseOxfordComma.Yes)
    {
      var count = values.Count();
      var comma = (useOxfordComma == UseOxfordComma.Yes) ? "," : "";

      if (count == 0)
        return "";
      else if (count == 1)
        return values.First();
      else if (count == 2)
        /* Special case: never use an Oxford comma when there are only two values. */
        return String.Join($" {connector} ", values);
      else
        return String.Join(", ", values.ToArray(), 0, count - 1) + $"{comma} {connector} {values.Last()}";
    }

    /// <summary>
    /// Concatenate the values in an <see cref="IEnumerable&lt;String&gt;"/>, placing a comma in between each value, except
    /// the word "and" is inserted between the last two values.
    /// <para>If <paramref name="values"/> is empty, an empty string is returned. If <paramref name="values"/> contains only one item,
    /// that item is returned.  If <paramref name="values"/> contains two items, the <paramref name="useOxfordComma"/> parameter is ignored,
    /// and those two items are returned separated by the word "and".</para>
    /// <br/>
    /// <br/>
    /// <example>
    /// Example:
    /// <code>
    /// var words = new List&lt;String&gt;() { "foo", "bar", "baz" };<br/>
    /// words.JoinAnd(); // "foo, bar, and baz"<br/>
    /// words.JoinAnd(UseOxfordComma.No); // "foo, bar and baz"
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="values">A collection that contains the strings to concatenate.</param>
    /// <param name="shouldUseOxfordComma">"Yes" (default) to place a comma before the second-to-last item.  "No" otherwise.</param>
    /// <returns>A <see cref="String"/> containing the concatenated items.</returns>
    public static String JoinAnd(this IEnumerable<String> values, UseOxfordComma useOxfordComma = UseOxfordComma.Yes) => JoinAndOr(values, "and", useOxfordComma);

    /// <summary>
    /// Concatenate the values in an <see cref="IEnumerable&lt;String&gt;"/>, placing a comma in between each value, except
    /// the word "or" is inserted between the last two values.
    /// <para>If <paramref name="values"/> is empty, an empty string is returned. If <paramref name="values"/> contains only one item,
    /// that item is returned.  If <paramref name="values"/> contains two items, the <paramref name="useOxfordComma"/> parameter is ignored,
    /// and those two items are returned separated by the word "or".</para>
    /// <br/>
    /// <br/>
    /// <example>
    /// Example:
    /// <code>
    /// var words = new List&lt;String&gt;() { "foo", "bar", "baz" };<br/>
    /// words.JoinOr(); // "foo, bar, or baz"<br/>
    /// words.JoinOr(UseOxfordComma.No); // "foo, bar or baz"
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="values">A collection that contains the strings to concatenate.</param>
    /// <param name="shouldUseOxfordComma">"Yes" (default) to place a comma before the second-to-last item.  "No" otherwise.</param>
    /// <returns>A <see cref="String"/> containing the concatenated items.</returns>
    public static String JoinOr(this IEnumerable<String> values, UseOxfordComma useOxfordComma = UseOxfordComma.Yes) => JoinAndOr(values, "or", useOxfordComma);

    /// <summary>
    /// Given an <see cref="IEnumerable&lt;String&gt;"/>, filter out all of the null or empty strings.
    /// </summary>
    /// <param name="values">An <see cref="IEnumerable&lt;String&gt;"/>.</param>
    /// <returns>A reference to the <see cref="IEnumerable&lt;String&gt;"/> with the null and empty strings filtered out.</returns>
    public static IEnumerable<String> RemoveNullOrEmpty(this IEnumerable<String> values) => values.Where(s => !String.IsNullOrEmpty(s));

    /// <summary>
    /// Given an <see cref="IEnumerable&lt;String&gt;"/>, filter out all of the null, empty, or whitespace-only strings.
    /// </summary>
    /// <param name="values">An <see cref="IEnumerable&lt;String&gt;"/>.</param>
    /// <returns>A reference to the <see cref="IEnumerable&lt;String&gt;"/> with the null, empty, and whitespace-only strings filtered out.</returns>
    public static IEnumerable<String> RemoveNullOrWhiteSpace(this IEnumerable<String> values) => values.Where(s => !String.IsNullOrWhiteSpace(s));

    /// <summary>
    /// Allow the lines of a text file to be lazily read from within a LINQ expression.  This can be useful if the file to be processed is very large.
    /// Essentially, this method is a lazy cousin to the <see cref="File.ReadAllLines"/> method.
    /// <br/>
    /// <br/>
    /// <example>
    /// Example:
    /// <code>
    /// using (var sr = new StreamReader(largeTextFile, true))<br/>
    /// {<br/>
    ///   var lines =<br/>
    ///     sr<br/>
    ///     .Lines()<br/>
    ///     .SkipWhile(line =&gt; (line != "&lt;XML&gt;") &amp;&amp; (line != "&lt;PAGE&gt;"))<br/>
    ///     .SkipWhile(line =&gt; String.IsNullOrWhiteSpace(line))<br/>
    ///     ...
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="textReader">A <see cref="TextReader"/> or one of its descendents.</param>
    /// <returns>Lazily returns the lines in the file as an <see cref="IEnumerable&lt;String&gt;"/>.</returns>
    public static IEnumerable<String> Lines(this TextReader textReader)
    {
      String line;
      while ((line = textReader.ReadLine()) != null)
        yield return line;
    }

    /// <summary>
    /// Return true if an <see cref="IEnumerable&lt;T&gt;"/> is null or contains no elements.
    /// </summary>
    /// <typeparam name="T">Any type.</typeparam>
    /// <param name="items">An <see cref="IEnumerable&lt;T&gt;"/>.  It may be null.</param>
    /// <returns>A <see cref="Boolean"/> true if <paramref name="items"/> is either null or contains no items.  False otherwise.</returns>
    public static Boolean IsNullOrEmpty<T>(this IEnumerable<T> items) => ((items == null) || !items.Any());

    /// <summary>
    /// Computes the product of a sequence of <see cref="Int32"/> values.
    /// </summary>
    /// <param name="ints">A sequence of <see cref="Int32"/> values to calculate the product of.</param>
    /// <returns>The product of the values in the sequence.</returns>
    public static BigInteger Product(this IEnumerable<Int32> ints) =>
      ints.Any()
      ? ints.Aggregate((BigInteger) 1, (acc, next) => acc * next)
      : 0;

    /// <summary>
    /// Splits an <see cref="IEnumerable&lt;T&gt;"/> into <paramref name="numOfParts"/>.
    /// </summary>
    /// <typeparam name="T">Any type.</typeparam>
    /// <param name="items">An <see cref="IEnumerable&lt;T&gt;"/>.</param>
    /// <param name="numOfParts">An <see cref="Int32"/>.  Must be greater than zero.</param>
    /// <returns>A nested IEnumerable (i.e. <see cref="IEnumerable&lt;IEnumerable&lt;T&gt;&gt;"/>).</returns>
    public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> items, Int32 numOfParts)
    {

      var i = 0;
      return items.GroupBy(x => i++ % numOfParts);
    }

    private static readonly Random _random = new();

    /// <summary>
    /// Randomize (swap the elements) of <paramref name="list"/> in place.
    /// </summary>
    /// <typeparam name="T">Any type.</typeparam>
    /// <param name="list">An <see cref="IList&lt;T&gt;"/>.</param>
    /// <returns>The same list, but with its elements randomly swapped.</returns>
    public static IList<T> RandomizeInPlace<T>(this IList<T> list)
    {
      for (var currentIndex = list.Count - 1; currentIndex >= 1; currentIndex--)
      {
        var randomIndex = _random.Next(currentIndex + 1);
        T value = list[randomIndex];
        list[randomIndex] = list[currentIndex];
        list[currentIndex] = value;
      }

      return list;
    }

    private static readonly Regex _digits = new(@"\d+");

    /* Code for OrderByNatural<T> is from StackOverflow answer https://stackoverflow.com/a/22323356/116198
       posted by Michael Parker (https://stackoverflow.com/users/1554346/michael-parker).

       Modifications: I moved the regex outside of the method and made it static, and changed some identifier names.

       Licensed under CC BY-SA 3.0 (https://creativecommons.org/licenses/by-sa/3.0/)
       See https://stackoverflow.com/help/licensing for more info. */
    public static IEnumerable<T> OrderByNatural<T>(this IEnumerable<T> items, Func<T, String> selector, StringComparer stringComparer = null)
    {
      var maxDigits =
        items
        .SelectMany(i => _digits.Matches(selector(i)).Cast<Match>().Select(digitChunk => (Int32?) digitChunk.Value.Length))
        .Max() ?? 0;

      return
        items
        .OrderBy(i => _digits.Replace(selector(i), match => match.Value.PadLeft(maxDigits, '0')), stringComparer ?? StringComparer.CurrentCulture);
    }

    /// <summary>
    /// Apply System.String.Trim() to all members of <paramref name="strings"/>.
    /// </summary>
    /// <param name="strings">An IEnumerable containing strings.</param>
    /// <returns>An IEnumerable of the trimmed strings.</returns>
    public static IEnumerable<String> Trim(this IEnumerable<String> strings) => strings.Select(s => s.Trim());

    /// <summary>
    /// Returns all of the values in <paramref name="enumValues"/> as an IEnumerable&lt;String&gt;.
    /// </summary>
    /// <typeparam name="T">An enumeration type.</typeparam>
    /// <param name="enumValues">A System.Enum value.</param>
    /// <returns>An enumeration of the values in <paramref name="enumValues"/>.</returns>
    public static IEnumerable<String> ToEnumerable<T>(this T enumValues)
      where T : Enum =>
      enumValues.ToString().Split(',').Select(s => s.Trim());
  }
}
