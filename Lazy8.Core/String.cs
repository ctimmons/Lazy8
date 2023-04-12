/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lazy8.Core;

public static partial class StringUtils
{
  /// <summary>
  /// Convert value to a MemoryStream, using a default Unicode encoding.
  /// <para>
  /// If <paramref name="value"/> is null, no data is written to the memory stream.
  /// If you intended to write a null byte to the memory stream, pass "\0"
  /// in the <paramref name="value"/> parameter.
  /// </para>
  /// </summary>
  public static MemoryStream ToMemoryStream(this String value) => value.ToMemoryStream(Encoding.Unicode);

  /// <summary>
  /// Convert value to a MemoryStream, using the given encoding.
  /// <para>
  /// If <paramref name="value"/> is null, no data is written to the memory stream.
  /// If you intended to write a null byte to the memory stream, pass "\0"
  /// in the <paramref name="value"/> parameter.
  /// </para>
  /// </summary>
  public static MemoryStream ToMemoryStream(this String value, Encoding encoding)
  {
    value.Name(nameof(value)).NotNull();
    encoding.Name(nameof(encoding)).NotNull();

    return new(encoding.GetBytes(value));
  }

  /// <summary>
  /// The <see cref="Regex.Escape()"/> method unconditionally escapes all regex metacharacters.
  /// <para>
  /// This method behaves like Regex.Escape(), but allows characters specified in <paramref name="charsToUnescape"/> to remain unescaped.
  /// </para>
  /// <para>If <paramref name="charsToUnescape"/> is null or empty, this method has the same behavior as Regex.Escape().</para>
  /// </summary>
  public static String RegexEscape(this String s, params Char[] charsToUnescape)
  {
    s.Name(nameof(s)).NotNullEmptyOrOnlyWhitespace();

    /* Start with a fuly escaped string. */
    var escapedRegex = Regex.Escape(s);

    if ((charsToUnescape == null) || (charsToUnescape.Length == 0))
    {
      return escapedRegex;
    }
    else
    {
      var result = new StringBuilder(escapedRegex.Length);
      var isRemovingEscapeCharacter = false;

      /* "Unescape" the characters in charsToUnescape. */
      for (var i = escapedRegex.Length - 1; i >= 0; i--)
      {
        var c = escapedRegex[i];

        if (isRemovingEscapeCharacter)
        {
          if (c != '\\')
            result.Insert(0, c);

          isRemovingEscapeCharacter = false;
        }
        else if (charsToUnescape.Contains(c))
        {
          result.Insert(0, c);
          isRemovingEscapeCharacter = true;
        }
        else
        {
          result.Insert(0, c);
        }
      }

      return result.ToString();
    }
  }

  /// <summary>
  /// Convert a standard Windows file mask string (i.e. a string containing '*' or '?' characters)
  /// into an equivalent Regex object.  If neither of those characters are in filemask, throw an error.
  /// </summary>
  public static Regex GetRegexFromFilemask(this String filemask)
  {
    filemask.Name(nameof(filemask)).NotNullEmptyOrOnlyWhitespace();

    if (!filemask.Intersect("*?".ToCharArray()).Any())
      throw new ArgumentException(String.Format(Properties.Resources.StringUtils_RegexFilemask, nameof(filemask), nameof(filemask)));

    var filemaskRegexPattern =
      filemask
      /* Escape all regex-related characters except '*' and '?'. */
      .RegexEscape('*', '?')
      /* Convert '*' and '?' to their regex equivalents. */
      .Replace('?', '.')
      .Replace("*", ".*?");

    return new Regex($"^{filemaskRegexPattern}$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
  }

  /// <summary>
  /// Returns the string s with all of the characters in cs removed.
  /// </summary>
  public static String Strip(this String s, Char[] cs)
  {
    s.Name(nameof(s)).NotNull();
    cs.Name(nameof(cs)).NotNull();

    return
      s
      .Where(c => !cs.Any(a => a == c))
      .Join();
  }

  /// <summary>
  /// Returns the first string parameter that is not null, has a length greater
  /// than zero, and does not consist only of whitespace.
  /// <para>Note how this behavior differs from the C# ?? null coalescing operator.</para>
  /// </summary>
  public static String Coalesce(params String[] strings)
  {
    strings.Name(nameof(strings)).NotNull();

    foreach (var s in strings)
      if (!s.IsNullOrWhiteSpace()) /* This works even if s is null, as null is strongly typed in C#. */
        return s;

    throw new ArgumentException(Properties.Resources.StringUtils_Coalesce);
  }

  /// <summary>
  /// Returns the beginning portion of s up to, but not including,
  /// the first occurrence of the character c.  If c is not present in
  /// s, then s is returned.
  /// </summary>
  public static String UpTo(this String s, Char c)
  {
    s.Name(nameof(s)).NotNull();

    return s.TakeWhile(ch => ch != c).Join();
  }

  /// <summary>
  /// Returns Char '1' for true, '0' for false.
  /// </summary>
  public static Char As0Or1(this Boolean value) => (value ? '1' : '0');

  /// <summary>
  /// Returns Char 'Y' for true, 'N' for false.
  /// </summary>
  public static Char AsYOrN(this Boolean value) => (value ? 'Y' : 'N');

  /// <summary>
  /// Returns true if value is in the set "1,Y,T,TRUE,YES" (case insensitive).
  /// <para>Any other value, including if value is null or empty, will return false.</para>
  /// </summary>
  public static Boolean AsBoolean(this String value) =>
    (!String.IsNullOrEmpty(value)) &&
    ContainsCI(Properties.Resources.StringUtils_BooleanTruthLiterals, value.Trim());

  /// <summary>
  /// Case insensitive (CI) version of String.IndexOf().
  /// </summary>
  public static Int32 IndexOfCI(this String source, String searchValue)
  {
    source.Name(nameof(source)).NotNull();
    searchValue.Name(nameof(searchValue)).NotNull();

    return source.IndexOf(searchValue, StringComparison.CurrentCultureIgnoreCase);
  }

  /// <summary>
  /// Case insensitive (CI) version of String.Contains().
  /// </summary>
  public static Boolean ContainsCI(this String source, String searchValue) => (source.IndexOfCI(searchValue) > -1);

  private static readonly ConcurrentDictionary<(String value, Int32 count), String> _memoizedRepeatStrings = new();

  /// <summary>
  /// Return <paramref name="value"/> repeated <paramref name="count"/> times.
  /// <para>
  /// <ul>
  ///   <li><paramref name="value"/> cannot be null</li>
  ///   <li><paramref name="count"/> must be greater than zero.</li>
  ///   <li>A <paramref name="count"/> of one returns value.</li>
  ///   <li>A <paramref name="count"/> of more than one returns value repeated <paramref name="count"/> times.</li>
  ///   <li>If (<paramref name="value"/>.Length * <paramref name="count"/>) > Int32.Max, an OverflowException is thrown.</li>
  /// </ul>
  /// </para>
  /// </summary>
  public static String Repeat(this String value, Int32 count)
  {
    value.Name(nameof(value)).NotNull();
    count.Name(nameof(count)).GreaterThanOrEqualTo(0);

    /* This method may be called frequently with the same parameters.

       Use the parameters to memoize the generated strings in a concurrent dictionary.
       This will reduce garbage collection pressure by not creating a new StringBuilder()
       instance unless it's absolutely necessary.

       Use the lambda overload of GetOrAdd() to ensure a new StringBuilder is only
       created when this call's parameter values aren't found in the dictionary. */

    if (count == 0)
      return "";
    else if (count == 1)
      return value;
    else
      return _memoizedRepeatStrings.GetOrAdd((value, count), _ => (new StringBuilder(value.Length * count)).Insert(0, value, count).ToString());
  }

  /// <summary>
  /// Return the last word - separated by a space character - in <paramref name="value"/>
  /// The returned value's leading and trailing spaces will be removed.
  /// <para>If there are no words in value (i.e. it's empty or has no spaces between non-space characters), then value will be returned unchanged.</para>
  /// <para>
  /// <paramref name="value"/> must be non-null.
  /// </para>
  /// </summary>
  public static String LastWord(this String value)
  {
    value.Name(nameof(value)).NotNull();

    var lastIndexOfSpace = value.Trim().LastIndexOf(' ');
    return (lastIndexOfSpace == -1) ? value : value[(lastIndexOfSpace + 1)..].Trim();
  }

  /// <summary>
  /// Returns <paramref name="value"/> with <paramref name="c"/> pre-pended and appended.
  /// <para>
  /// Both <paramref name="value"/> and <paramref name="c"/> must be non-null.
  /// </para>
  /// </summary>
  public static String SurroundWith(this String value, String c) => value.SurroundWith(c, c);

  /// <summary>
  /// Returns <paramref name="value"/> with <paramref name="c1"/>' pre-pended and <paramref name="c2"/>' appended.
  /// <para>
  /// All parameters must be non-null.
  /// </para>
  /// </summary>
  public static String SurroundWith(this String value, String c1, String c2)
  {
    value.Name(nameof(value)).NotNull();
    c1.Name(nameof(c1)).NotNull();
    c2.Name(nameof(c2)).NotNull();

    return String.Concat(c1, value, c2);
  }

  /// <summary>
  /// Return <paramref name="value"/> surrounded by single quotes.
  /// </summary>
  /// <param name="value">A string (not null).</param>
  /// <returns></returns>
  public static String SingleQuote(this String value) => value.SurroundWith("'");

  /// <summary>
  /// Return <paramref name="value"/> surrounded by double quotes.
  /// </summary>
  /// <param name="value">A string (not null).</param>
  /// <returns></returns>
  public static String DoubleQuote(this String value) => value.SurroundWith("\"");

  /// <summary>
  /// Return <paramref name="value"/> surrounded by square brackets (i.e. [ and ]).
  /// </summary>
  /// <param name="value">A string (not null).</param>
  /// <returns></returns>
  public static String SquareBrackets(this String value) => value.SurroundWith("[", "]");

  /// <summary>
  /// Return the MD5 checksum for <paramref name="value"/>, using an ASCII encoding for <paramref name="value"/>.
  /// <para>
  /// <paramref name="value"/> must be non-null.
  /// </para>
  /// </summary>
  public static String MD5Checksum(this String value) => MD5Checksum(value, Encoding.ASCII);

  /// <summary>
  /// Return the MD5 checksum for <paramref name="value"/>, using an ASCII encoding for <paramref name="value"/>.
  /// <para>
  /// <paramref name="value"/> must be non-null.
  /// </para>
  /// </summary>
  public static Byte[] MD5ChecksumAsByteArray(this String value) => MD5ChecksumAsByteArray(value, Encoding.ASCII);

  /// <summary>
  /// Return the MD5 checksum for <paramref name="value"/>, using <paramref name="encoding"/> as an encoding for <paramref name="value"/>.
  /// <para>
  /// Both <paramref name="value"/> and <paramref name="encoding"/> must be non-null.
  /// </para>
  /// </summary>
  public static String MD5Checksum(this String value, Encoding encoding)
  {
    value.Name(nameof(value)).NotNull();
    encoding.Name(nameof(encoding)).NotNull();

    using (var ms = new MemoryStream(encoding.GetBytes(value)))
      return ms.MD5Checksum();
  }

  /// <summary>
  /// Return the MD5 checksum for <paramref name="value"/>, using <paramref name="encoding"/> as an encoding for <paramref name="value"/>.
  /// <para>
  /// Both <paramref name="value"/> and <paramref name="encoding"/> must be non-null.
  /// </para>
  /// </summary>
  public static Byte[] MD5ChecksumAsByteArray(this String value, Encoding encoding)
  {
    value.Name(nameof(value)).NotNull();
    encoding.Name(nameof(encoding)).NotNull();

    using (var ms = new MemoryStream(encoding.GetBytes(value)))
      return ms.MD5ChecksumAsByteArray();
  }

  /// <summary>
  /// Remove <paramref name="prefix"/> from the beginning of <paramref name="value"/> and return the result.
  /// If <paramref name="value"/> doesn't start with <paramref name="prefix"/>, <paramref name="value"/> is returned unchanged.
  /// <para>
  /// The comparison is case-sensitive. Both <paramref name="value"/> and <paramref name="prefix"/> must be non-null.
  /// </para>
  /// </summary>
  public static String TrimStart(this String value, String prefix)
  {
    value.Name(nameof(value)).NotNull();
    prefix.Name(nameof(prefix)).NotNull();

    if (!value.Any() || !prefix.Any())
      return value;
    else if (value.StartsWith(prefix, StringComparison.CurrentCulture))
      return value[prefix.Length..];
    else
      return value;
  }

  /// <summary>
  /// Remove <paramref name="suffix"/> from the end of <paramref name="value"/> and return the result.
  /// If <paramref name="value"/> doesn't end with <paramref name="suffix"/>, <paramref name="value"/> is returned unchanged.
  /// <para>
  /// The comparison is case-sensitive. Both <paramref name="value"/> and <paramref name="suffix"/> must be non-null.
  /// </para>
  /// </summary>
  public static String TrimEnd(this String value, String suffix)
  {
    value.Name(nameof(value)).NotNull();
    suffix.Name(nameof(suffix)).NotNull();

    if (!value.Any() || !suffix.Any())
      return value;
    else if (value.EndsWith(suffix, StringComparison.CurrentCulture))
      return value.Substring(0, value.Length - suffix.Length);
    else
      return value;
  }

  /// <summary>
  /// Remove <paramref name="stringToTrim"/> from the beginning and end of <paramref name="value"/> and return the result.
  /// If <paramref name="value"/> doesn't both start and end with <paramref name="stringToTrim"/>, <paramref name="value"/> is returned unchanged.
  /// <para>
  /// The comparison is case-sensitive. Both <paramref name="value"/> and <paramref name="stringToTrim"/> must be non-null.
  /// </para>
  /// </summary>
  public static String RemovePrefixAndSuffix(this String value, String stringToTrim) =>
    value.TrimStart(stringToTrim).TrimEnd(stringToTrim);

  /// <summary>
  /// If <paramref name="value"/> doesn't already end with a trailing slash, one is appended to <paramref name="value"/> and the combined string is returned.
  /// <para>
  /// <paramref name="value"/> must be non-null.
  /// </para>
  /// </summary>
  public static String AddTrailingForwardSlash(this String value)
  {
    value.Name(nameof(value)).NotNull();

    return value.EndsWith("/") ? value : value + "/";
  }

  [GeneratedRegex("<[^>]+?>", RegexOptions.Singleline)]
  private static partial Regex StripHtmlRegex();

  /// <summary>
  /// Remove anything that resembles an HTML or XML tag from <paramref name="value"/> and return the modified string.
  /// <para>
  /// <paramref name="value"/> must be non-null.
  /// </para>
  /// </summary>
  public static String RemoveHtml(this String value)
  {
    value.Name(nameof(value)).NotNull();

    return StripHtmlRegex().Replace(value, "");
  }

  [GeneratedRegex(@"[\p{Z}\p{C}]" /* All Unicode whitespace (Z) and control characters (C). */, RegexOptions.Singleline)]
  private static partial Regex WhitespaceRegex();

  /// <summary>
  /// Remove all Unicode whitespace and control characters from <paramref name="value"/> and return the modified string.
  /// <para>
  /// <paramref name="value"/> must be non-null.
  /// </para>
  /// </summary>
  public static String RemoveWhitespace(this String value)
  {
    value.Name(nameof(value)).NotNull();

    return WhitespaceRegex().Replace(value, "");
  }

  /// <summary>
  /// Returns true if <paramref name="value"/> is null, has a length of zero, or contains only whitespace.
  /// <para>(This method just provides a fluent interface to String.IsNullOrWhiteSpace().)</para>
  /// </summary>
  public static Boolean IsNullOrWhiteSpace(this String value) => String.IsNullOrWhiteSpace(value);

  /// <summary>
  /// Returns true if <paramref name="value"/> is not null, has a length greater than zero, and does not consist of only whitespace.
  /// </summary>
  public static Boolean IsNotNullOrWhiteSpace(this String value) => !value.IsNullOrWhiteSpace();

  /// <summary>
  /// Returns true if all strings in <paramref name="values"/> are null, have a length of zero, or contain only whitespace.
  /// <para>
  /// <paramref name="values"/> must be non-null.
  /// </para>
  /// </summary>
  public static Boolean AreAllEmpty(this List<String> values)
  {
    values.Name(nameof(values)).NotNull();

    return values.All(s => s.IsNullOrWhiteSpace());
  }

  /// <summary>
  /// Returns true if any of the strings in <paramref name="values"/> are null, have a length of zero, or contain only whitespace.
  /// <para>
  /// <paramref name="values"/> must be non-null.
  /// </para>
  /// </summary>
  public static Boolean AreAnyEmpty(this List<String> values)
  {
    values.Name(nameof(values)).NotNull();

    return values.Any(s => s.IsNullOrWhiteSpace());
  }

  [GeneratedRegex("(\r\n|\n)")]
  private static partial Regex IndentTextRegex();

  /// <summary>
  /// Treat <paramref name="value"/> as a multiline string, where each string is separated either by
  /// a carriage return/linefeed combo, or just a linefeed.
  /// <para>If the <paramref name="indent"/> parameter is zero, <paramref name="value"/> is returned unchanged.</para>
  /// <para>If the <paramref name="indent"/> parameter is greater than zero, each line in <paramref name="value"/> is indented by <paramref name="indent"/> spaces and the modified string is returned.</para>
  /// <para>If the <paramref name="indent"/> parameter is less than zero, each line in <paramref name="value"/> is unindented by <paramref name="indent"/> spaces and the modified string is returned. If a line has fewer leading spaces than <paramref name="indent"/>, the line is unchanged.</para>
  /// <para><paramref name="value"/> must be non-null</para>
  /// </summary>
  public static String Indent(this String value, Int32 indent)
  {
    value.Name(nameof(value)).NotNull();

    if (indent == 0)
      return value;

    var indentString = " ".Repeat(Math.Abs(indent));

    if (indent > 0)
    {
      return indentString + IndentTextRegex().Replace(value, "$1" + indentString);
    }
    else /* if (indent < 0) */
    {
      indent = Math.Abs(indent);

      /* Since the regex has capturing parentheses, any matching /r and /n characters
         will be included in the results returned by the Regex.Split() method.  See the Regex.Split() MSDN documentation at:
         https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.split?view=net-5.0#System_Text_RegularExpressions_Regex_Split_System_String_ */
      return
        IndentTextRegex()
        .Split(value)
        .Select(line => line.StartsWith(indentString) ? line[indent..] : line)
        .Join();
    }
  }

  /// <summary>
  /// Case-insensitive version of String.Equals().
  /// </summary>
  public static Boolean EqualsCI(this String value, String other) => value.Equals(other, StringComparison.CurrentCultureIgnoreCase);

  /// <summary>
  /// Case-insensitive version of String.StartsWith().
  /// </summary>
  public static Boolean StartsWithCI(this String value, String other) => value.StartsWith(other, StringComparison.CurrentCultureIgnoreCase);

  /// <summary>
  /// Replace all solitary carriage returns (\r) and line feeds (\n) with carriage return/linefeed pairs (\r\n).
  /// Existing carriage return/linefeed pairs (\r\n) are unaffected.
  /// </summary>
  /// <param name="source">Source string</param>
  /// <returns>Modified string</returns>
  public static String CRLF(this String source)
  {
    source.Name(nameof(source)).NotNull();

    /* This code is the functional equivalent of:

         var crlfRegex = new Regex(@"\r(?!\n)|(?<!\r)\n", RegexOptions.Compiled);
         return crlfRegex.Replace(source, "\r\n");

       However, this code runs about 10x faster than the regex. */

    var result = new StringBuilder(source.Length);
    var n = 0;

    while (n < source.Length)
    {
      if (source[n] == '\r')
      {
        if (n < (source.Length - 1))
        {
          if (source[n + 1] == '\n')
          {
            n += 2;
          }
          else
          {
            n++;
          }
        }
        else
        {
          n++;
        }

        result.Append("\r\n");
      }
      else if (source[n] == '\n')
      {
        result.Append("\r\n");
        n++;
      }
      else
      {
        result.Append(source[n]);
        n++;
      }
    }

    return result.ToString();
  }

  /// <summary>
  /// Replace all solitary carriage returns (\r) and carriage return/linefeed pairs (\r\n) with line feeds (\n).
  /// Existing linefeeds (\n) are unaffected.
  /// </summary>
  /// <param name="source">Source string</param>
  /// <returns>Modified string</returns>
  public static String LF(this String source)
  {
    source.Name(nameof(source)).NotNull();

    /* This code is the functional equivalent of:

         var lfRegex = new Regex(@"\r\n?", RegexOptions.Compiled);
         return lfRegex.Replace(source, "\n");

       However, this code runs about 10x faster than the regex. */

    if (source == null)
      return null;

    var result = new StringBuilder(source.Length);
    var n = 0;

    while (n < source.Length)
    {
      if (source[n] == '\r')
      {
        if (n < (source.Length - 1))
        {
          if (source[n + 1] == '\n')
          {
            n += 2;
          }
          else
          {
            n++;
          }
        }
        else
        {
          n++;
        }

        result.Append('\n');
      }
      else
      {
        result.Append(source[n]);
        n++;
      }
    }

    return result.ToString();
  }

  /// <summary>
  /// Remove a single linefeed (\n), carriage return (\r), or carriage return/linefeed pair (\r\n) from
  /// the end of <paramref name="source"/>.  If none of those characters exist at the end of <paramref name="source"/>,
  /// it is returned unchanged.
  /// <para>This is a port of Perl's chomp() function.</para>
  /// </summary>
  /// <param name="source">Source string</param>
  /// <returns>Modified string</returns>
  public static String Chomp(this String source)
  {
    source.Name(nameof(source)).NotNull();

    /* Emulate Perl's chomp() function.
       Remove a single \n, \r, or single \r\n pair from the end of a string.

       Can't use String.TrimEnd(array) because it doesn't discriminate;
       it removes all of the characters in 'array' - regardless of order -
       from the end of a string.

       For example:

         "x\n\n\n".TrimEnd("\n".ToCharArray()) returns "x", whereas 

         "x\n\n\n".Chomp() returns "x\n\n".

       It gets worse when \r\n combos are present.

         "x\n\r\n\r\n".TrimEnd("\r\n".ToCharArray()) returns "x", whereas 

         "x\n\r\n\r\n".Chomp() returns "x\n\r\n".

       There's a neat trick on StackOverflow, but it only works on single-line
       strings that end with a single trailing \r, \n, or \r\n pair.
       (https://stackoverflow.com/a/1038072/116198).

          new StringReader("x\n").ReadLine() returns "x", but

          new StringReader("x\n\n").ReadLine() also returns "x", and

          new StringReader("x\n\nHello\nWorld!\n").ReadLine() returns "x" as well.

       In all cases, Chomp() acts intelligently and only removes the
       final \r, \n, or \r\n pair if those characters are present.
       Otherwise Chomp() returns the original string. */

    /* Maintain a piece of state to prevent consuming multiple \n characters. */
    var previouslyConsumedALinefeed = false;

    /* Process the string backwards. */
    var n = source.Length - 1;
    while (n >= 0)
    {
      var ch = source[n];

      if (!previouslyConsumedALinefeed && (ch == '\n'))
      {
        /* Consume the \n. */
        n--;

        /* Don't consume another \n on the next iteration. */
        previouslyConsumedALinefeed = true;

        /* Continue and see if the previous character is the \r in a \r\n combo. */
        continue;
      }
      else if (ch == '\r')
      {
        /* Consume the \r. */
        n--;

        /* Done.  Whether this \r was a solitary \r, or the \r in a \r\n combo,
           there's nothing more to consume. */
        break;
      }
      else
      {
        break;
      }
    }

    return
      (n == source.Length - 1)      /* No chomping was done, */
      ? source                      /* so just return the original string. O(1) performance. */
      : source.Substring(0, n + 1); /* Otherwise return the appropriate substring. O(n) performance because of the Substring() call. */
  }

  /// <summary>
  /// Given a <paramref name="series"/> <see cref="String"/> that contains one or more positive numbers separated by commas,
  /// and/or one or more ranges of positive numbers separated by a dash, return an <see cref="IEnumerable&lt;Int32&gt;"/>
  /// containing a list of all of the numbers represented in <paramref name="series"/>.
  /// <para>
  /// <b>Examples:</b>
  /// <code>
  /// "".GetSeries(); // Returns an empty IEnumerable&lt;Int32&gt;<br/>
  /// "42".GetSeries(); // Returns IEnumerable&lt;Int32&gt; { 42 }<br/>
  /// "3-6".GetSeries(); // Returns IEnumerable&lt;Int32&gt; { 3, 4, 5, 6 }<br/>
  /// "42, 3-6".GetSeries(); // Returns IEnumerable&lt;Int32&gt; { 42, 3, 4, 5, 6 }<br/>
  /// "42, 3-6, 77, 9-12".GetSeries(); // Returns IEnumerable&lt;Int32&gt; { 42, 3, 4, 5, 6, 77, 9, 10, 11, 12 }<br/>
  /// </code>
  /// </para>
  /// </summary>
  /// <param name="series">A <see cref="String"/>.</param>
  /// <param name="shouldNormalizeOverlaps">A <see cref="Boolean"/> indicating if duplicates resulting from overlapping ranges should be eliminated (true) or not (false).  Defaults to false.</param>
  /// <returns>An <see cref="IEnumerable&lt;Int32&gt;"/>.</returns>
  public static IEnumerable<Int32> GetSeries(this String series, Boolean shouldNormalizeOverlaps = false)
  {
    series.Name(nameof(series)).NotNull();

    var result =
      series
      .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
      .Select(s => s.Trim())
      .Select(
        range =>
        {
          var elements = range.Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());

          if (elements.Count() == 1)
          {
            return new List<Int32>() { Convert.ToInt32(elements.First()) };
          }
          else if (elements.Count() == 2)
          {
            var first = Convert.ToInt32(elements.First());
            var last = Convert.ToInt32(elements.Last());
            return Enumerable.Range(first, (last - first) + 1);
          }
          else
          {
            throw new FormatException(String.Format(Properties.Resources.StringUtils_BadRangeFormat, range));
          }
        })
      .SelectMany(x => x);

    return shouldNormalizeOverlaps ? result.Distinct() : result;
  }

  public static String ChopBeginningAndEnd(this String s, Int32 n = 1)
  {
    s.Name(nameof(s)).NotNull();

    return s[n..^n];
  }
}

