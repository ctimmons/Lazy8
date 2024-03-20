/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lazy8.Core;

[Flags]
public enum StringAssertion
{
  None = 0x1,
  NotNull = 0x2,
  NotOnlyWhitespace = 0x4,
  NotZeroLength = 0x8,
  All = NotNull | NotOnlyWhitespace | NotZeroLength
}

/*

  A small collection of extension methods which reduce the code needed to
  check a method's variables for pre-conditions before executing the method's
  core logic.

  A common idiom in C# methods is using one or more if/then statements to check parameter(s) for validity.

    public String GetFileContents(String filename)
    {
      if (String.IsNullOrWhiteSpace(filename))
        throw new ArgumentNullException($"'{nameof(filename)}' cannot be null, empty, or contain only whitespace characters.");

      if (!File.Exists(filename))
        throw new ArgumentException($"'{nameof(filename)}' must exist.");

      // execute core logic here...
    }

  Those if/then statements are ripe for abstraction.
  A more pleasant way to express the above logic might be something like this:

    public String GetFileContents(String filename)
    {
      filename.Name(nameof(filename)).NotNullEmptyOrOnlyWhitespace().FileExists();

      // execute core logic here...
    }

  That's what this class provides - extension methods on .Net's IEnumerable and IComparable
  interfaces that reduce many of those if/then statements to a simple chain of function calls.

  One thing to note is these string extension methods will work if the type is null.

    // The following lines of code are equivalent.

    String? s = null;  s!.Name(nameof(s)).NotNull();

    ((String) null!).Name(nameof(s)).NotNull();


  See the unit tests in Lazy8.Core.Tests/Assert.cs for usage examples.

*/

public static partial class AssertUtils
{
  public static AssertionContext<T> Name<T>(this T source, String name) => new(name, source);

  public static AssertionContext<String> NotOnlyWhitespace(this String source) => (new AssertionContext<String>(source)).NotOnlyWhitespace();

  public static AssertionContext<String> NotOnlyWhitespace(this AssertionContext<String> source) =>
    source.Value.Trim() == ""
    ? throw new ArgumentException(String.Format(Properties.Resources.Assert_StringIsAllWhitespace, source.Name))
    : source;

  public static AssertionContext<String> NotNullEmptyOrOnlyWhitespace(this String source) =>
    (new AssertionContext<String>(source)).NotNullEmptyOrOnlyWhitespace();

  public static AssertionContext<String> NotNullEmptyOrOnlyWhitespace(this AssertionContext<String> source) =>
    source.NotNull().NotEmpty().NotOnlyWhitespace();

  public static AssertionContext<T> NotNull<T>(this T source)
    where T : class =>
    (new AssertionContext<T>(source)).NotNull();

  public static AssertionContext<T> NotNull<T>(this AssertionContext<T> source)
    where T : class =>
    source.Value is null ? throw new ArgumentNullException(source.Name) : source;

  public static AssertionContext<T> NotEmpty<T>(this T source)
    where T : IEnumerable =>
    (new AssertionContext<T>(source)).NotEmpty();

  public static AssertionContext<T> NotEmpty<T>(this AssertionContext<T> source)
    where T : IEnumerable
  {
    /* Some non-generic IEnumerator instances returned by IEnumerable.GetEnumerator()
       implement IDisposable, while others do not.  Those enumerators
       that do implement IDisposable will need to have their Dispose() method called.

       A non-generic IEnumerator cannot be used in a "using" statement.
       So to make sure Dispose() is called (if it exists), "foreach" is used
       because it will generate code to dispose of the IEnumerator
       if the enumerator implements IDisposable. */

    /* This loop will execute zero or more times,
       and the foreach will dispose the enumerator, if necessary. */
    foreach (var _ in source.Value)
      return source; /* Loop executed once.  There is at least one element in the IEnumerable, which means it's not empty. */

    /* Loop executed zero times, which means the IEnumerable is empty. */
    throw new ArgumentException(String.Format(Properties.Resources.Assert_ContainerIsEmpty, source.Name));
  }

  public static AssertionContext<T> NoStringsAreNullOrWhiteSpace<T>(this T source)
    where T : IEnumerable<String> =>
    (new AssertionContext<T>(source)).NoStringsAreNullOrWhiteSpace();

  public static AssertionContext<T> NoStringsAreNullOrWhiteSpace<T>(this AssertionContext<T> source)
    where T : IEnumerable<String> =>
    source.Value.Any(String.IsNullOrWhiteSpace)
    ? throw new ArgumentException(String.Format(Properties.Resources.Assert_OneOrMoreStringsAreEmpty, source.Name))
    : source;

  public static AssertionContext<T> GreaterThan<T>(this T source, T value)
    where T : IComparable<T> =>
    (new AssertionContext<T>(source)).GreaterThan(value);

  public static AssertionContext<T> GreaterThan<T>(this AssertionContext<T> source, T value)
    where T : IComparable<T> =>
    source.Value.CompareTo(value) > 0
    ? source
    : throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Assert_NotGreaterThan, source.Name, source.Value, value));

  public static AssertionContext<T> GreaterThanOrEqualTo<T>(this T source, T value)
    where T : IComparable<T> =>
    (new AssertionContext<T>(source)).GreaterThanOrEqualTo(value);

  public static AssertionContext<T> GreaterThanOrEqualTo<T>(this AssertionContext<T> source, T value)
    where T : IComparable<T> =>
    source.Value.CompareTo(value) >= 0
    ? source
    : throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Assert_NotGreaterThanOrEqualTo, source.Name, source.Value, value));

  public static AssertionContext<T> LessThan<T>(this T source, T value)
    where T : IComparable<T> =>
    (new AssertionContext<T>(source)).LessThan(value);

  public static AssertionContext<T> LessThan<T>(this AssertionContext<T> source, T value)
    where T : IComparable<T> =>
    source.Value.CompareTo(value) < 0
    ? source
    : throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Assert_NotLessThan, source.Name, source.Value, value));

  public static AssertionContext<T> LessThanOrEqualTo<T>(this T source, T value)
    where T : IComparable<T> =>
    (new AssertionContext<T>(source)).LessThanOrEqualTo(value);

  public static AssertionContext<T> LessThanOrEqualTo<T>(this AssertionContext<T> source, T value)
    where T : IComparable<T> =>
    source.Value.CompareTo(value) <= 0
    ? source
    : throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Assert_NotLessThanOrEqualTo, source.Name, source.Value, value));

  public static AssertionContext<T> EqualTo<T>(this T source, T value)
    where T : IComparable<T> =>
    (new AssertionContext<T>(source)).EqualTo(value);

  public static AssertionContext<T> EqualTo<T>(this AssertionContext<T> source, T value)
    where T : IComparable<T> =>
    source.Value.CompareTo(value) == 0
    ? source
    : throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Assert_NotEqualTo, source.Name, source.Value, value));

  public static AssertionContext<T> NotEqualTo<T>(this T source, T value)
    where T : IComparable<T> =>
    (new AssertionContext<T>(source)).NotEqualTo(value);

  public static AssertionContext<T> NotEqualTo<T>(this AssertionContext<T> source, T value)
    where T : IComparable<T> =>
    source.Value.CompareTo(value) != 0
    ? source
    : throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Assert_EqualTo, source.Name, source.Value, value));

  public static AssertionContext<T> BetweenInclusive<T>(this T source, T lowerBound, T upperBound)
    where T : IComparable<T> =>
    (new AssertionContext<T>(source)).BetweenInclusive(lowerBound, upperBound);

  public static AssertionContext<T> BetweenInclusive<T>(this AssertionContext<T> source, T lowerBound, T upperBound)
    where T : IComparable<T> =>
    (source.Value.CompareTo(lowerBound) >= 0) && (source.Value.CompareTo(upperBound) <= 0)
    ? source
    : throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Assert_BetweenInclusive, source.Name, source.Value, lowerBound, upperBound));

  public static AssertionContext<T> BetweenExclusive<T>(this T source, T lowerBound, T upperBound)
    where T : IComparable<T> =>
    (new AssertionContext<T>(source)).BetweenExclusive(lowerBound, upperBound);

  public static AssertionContext<T> BetweenExclusive<T>(this AssertionContext<T> source, T lowerBound, T upperBound)
    where T : IComparable<T> =>
    (source.Value.CompareTo(lowerBound) > 0) && (source.Value.CompareTo(upperBound) < 0)
    ? source
    : throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Assert_BetweenExclusive, source.Name, source.Value, lowerBound, upperBound));

  public static AssertionContext<String> DirectoryExists(this String source) => (new AssertionContext<String>(source)).DirectoryExists();

  public static AssertionContext<String> DirectoryExists(this AssertionContext<String> source) =>
    Directory.Exists(source.Value)
    ? source
    : throw new ArgumentException(String.Format(Properties.Resources.Assert_DirectoryExists, source.Name, source.Value));

  public static AssertionContext<DirectoryInfo> DirectoryExists(this DirectoryInfo source) => (new AssertionContext<DirectoryInfo>(source)).DirectoryExists();

  public static AssertionContext<DirectoryInfo> DirectoryExists(this AssertionContext<DirectoryInfo> source) =>
    source.Value.Exists
    ? source
    : throw new ArgumentException(String.Format(Properties.Resources.Assert_DirectoryExists, source.Name, source.Value));

  public static AssertionContext<String> FileExists(this String source) => (new AssertionContext<String>(source)).FileExists();

  public static AssertionContext<String> FileExists(this AssertionContext<String> source) =>
    File.Exists(source.Value)
    ? source
    : throw new ArgumentException(String.Format(Properties.Resources.Assert_FileExists, source.Name, source.Value));

  public static AssertionContext<FileInfo> FileExists(this FileInfo source) => (new AssertionContext<FileInfo>(source)).FileExists();

  public static AssertionContext<FileInfo> FileExists(this AssertionContext<FileInfo> source) =>
    source.Value.Exists
    ? source
    : throw new ArgumentException(String.Format(Properties.Resources.Assert_FileExists, source.Name, source.Value));
}

/// <summary>
/// Encapsulate a name and value of type T together, allowing the name/value pair to be propagated between methods.
/// </summary>
/// <typeparam name="T">Any type.</typeparam>
public class AssertionContext<T>
{
  public String Name { get; init; }
  public T Value { get; init; }

  public AssertionContext(T value)
  {
    this.Name = "<Unknown variable name>";
    this.Value = value;
  }

  public AssertionContext(String name, T value)
  {
    this.Name = name;
    this.Value = value;
  }
}

