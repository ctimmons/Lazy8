/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lazy8.Core;

/// <summary>
/// A general purpose lexical scanner.
/// <para>This class has some parallels with <see cref="System.IO.StringReader"/>, although the two are
/// not type compatible.</para>
/// <para>Note that this class is not thread-safe.  Attempting to peform asynchronous or parallel matches will
/// result in a race condition.</para>
/// </summary>
public class StringScanner
{
  public const Int32 END_OF_INPUT = -1;

  private readonly String _s;
  private readonly Int32 _length;

  /* _index is the current linear position of the scanner (i.e. treating _s as a one-dimensional vector).
     _line and _column portray _s as a two-dimensional ragged array.

     Note that _index, _line, and _column are all zero-based values. */
  private Int32 _index;
  private Int32 _line;
  private Int32 _column;
  private readonly Stack<(Int32 Position, Int32 Line, Int32 Column)> _positions = new();

  /// <summary>
  /// A tuple containing the scanner's current line and column.  Both of these values are one-based.
  /// </summary>
  public (Int32 Line, Int32 Column) Position => (this._line + 1, this._column + 1);

  private readonly StringComparison _stringComparison;

  /* Leave IsBof and IsEof as private - do not change their scope to public.

     While IsBof could be made public and not cause confusion, that is not true
     for IsEof.  IsEof violates the 'Principle of Least Surprise', and will just
     cause confusion in the caller's mind as to where StringScanner thinks
     the end of the input string really is.

     Technically, the *real* eof is at _length - 1.  That's the last valid
     index in _s.  But IsEof checks _length because of how the
     Read() method increments _index.  Altering this logic would just create a mess (I've tried).

     Besides, the user of StringScanner doesn't need to really know where the eof is
     because both the Read() and Peek() methods return END_OF_INPUT (-1) when they should (no surprising behavior).

     And there are the SavePosition(), GoBackToSavedPosition() and AcceptNewPosition() methods in case
     the caller needs to backtrack after an unsuccessful match. */

  private Boolean IsBof => (this._index == 0);
  private Boolean IsEof => (this._index == this._length);

  /* Zero-width assertions for beginning-of-line and end-of-line for logical lines within this._s. */

  /// <summary>
  /// Indicates whether the scanner's current position is at the beginning of a logical line.
  /// <para>The beginning of a logical line is considered to be when the scanner's position is at the start
  /// of the string, or if the character preceding the current position is a carriage return (\r) or line feed (\n).</para>
  /// </summary>
  public Boolean IsBol => this.IsBof || (Char) this.ReversePeek() == '\r' || (Char) this.ReversePeek() == '\n';

  /// <summary>
  /// Indicates whether the scanner's current position is at the end of a logical line.
  /// <para>The end of a logical line is considered to be when the scanner's position is at the end
  /// of the string, or if the character after the current position is a carriage return (\r) or line feed (\n).</para>
  /// </summary>
  public Boolean IsEol => this.IsEof || (Char) this.Peek() == '\r' || (Char) this.Peek() == '\n';

  private StringScanner() : base() { }

  /// <summary>
  /// Creates a new StringScanner instance.
  /// </summary>
  /// <param name="s">The <see cref="System.String"/> to be scanned.</param>
  /// <param name="isCaseSensitive">A <see cref="System.Boolean"/> value indicating whether any string comparisons should
  /// be case-sensitive. The current culture is used when comparing strings.</param>
  public StringScanner(String s, Boolean isCaseSensitive = false) : this()
  {
    this._s = s ?? throw new ArgumentException(String.Format(Properties.Resources.StringScanner_NullConstructorValue, nameof(s)));
    this._length = s.Length;
    this._stringComparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
  }

  /// <summary>
  /// Reads the next character from the input string.
  /// </summary>
  /// <returns>An integer representing the character read from the underlying string,
  /// or END_OF_INPUT (-1) if no more characters are available.</returns>
  public Int32 Read()
  {
    var currentChar = this.Peek();

    if (currentChar == END_OF_INPUT)
      return currentChar;

    this._index++;

    /* When a new line is encountered, the _line and _column fields have to be updated.
       That requires some extra logic to correctly handle all three of these cases.

       Strings can contain three different kinds of new lines:

         a carriage return/line feed combo (\r\n) (all versions of Windows),
         a solitary line feed (\n) (Unix, Linux, MacOS (aka OSX), and recent versions of Windows), or
         a solitary carriage return (\r) (classic MacOS) */

    if ((Char) currentChar == '\r')
    {
      /* Is currentChar \r the first part of a \r\n combo? */
      var nextChar = this.Peek();
      if ((nextChar != END_OF_INPUT) && ((Char) nextChar == '\n'))
      {
        /* Yes. Just increment _column and leave _line unchanged. */
        this._column++;
      }
      else
      {
        /* No, currentChar is a solitary \r, or the end of the string has been reached.
           Increment _line and reset _column to zero. */
        this._line++;
        this._column = 0;
      }
    }
    else if ((Char) currentChar == '\n')
    {
      /* currentChar is either a solitary \n, or the \n in a \r\n combo.
         In either case, increment _line and reset _column to zero. */
      this._line++;
      this._column = 0;
    }
    else
    {
      /* Just an ordinary character. Increment _column. */
      this._column++;
    }

    return currentChar;
  }

  /// <summary>
  /// Returns the next available character but does not consume it.
  /// </summary>
  /// <returns>An integer representing the next character to be read, or END_OF_INPUT (-1) if at the end of the string.</returns>
  public Int32 Peek() => this.IsEof ? END_OF_INPUT : this._s[this._index];

  /// <summary>
  /// Returns the previously available character but does not consume it.
  /// </summary>
  /// <returns>Returns an integer representing the character immediately prior to the scanner's current position,
  /// or END_OF_INPUT (-1) if at the beginning of the string.</returns>
  public Int32 ReversePeek() => this.IsBof ? END_OF_INPUT : this._s[this._index - 1];

  /// <summary>
  /// Match zero or more characters as long as the <paramref name="predicate"/> returns true.
  /// <para>This is analogous to LINQ's TakeWhile() method.</para>
  /// </summary>
  /// <param name="predicate">A method that takes a Char and returns a Boolean.</param>
  /// <returns>A String with zero or more matched characters.</returns>
  public String MatchWhile(Predicate<Char> predicate)
  {
    StringBuilder result = new();
    Int32 nextChar;
    while (((nextChar = this.Peek()) != END_OF_INPUT) && predicate((Char) nextChar))
      result.Append((Char) this.Read());
    return result.ToString();
  }

  /// <summary>
  /// Attempts to completely match <paramref name="s"/> to the portion of the scanner's string starting at the scanner's current position.
  /// </summary>
  /// <param name="s">The string to match.  Null and empty strings will result in a failed match.  As will strings
  /// that are longer than the remaining unmatched portion of the scanner's string.</param>
  /// <returns>A Boolean indicating if the match was succesful.</returns>
  public Boolean MatchLiteral(String s)
  {
    if ((s is null) || !s.Any() || ((this._index + s.Length) > this._length))
      return false;

    this.SavePosition();

    var sPosition = 0;
    Int32 nextChar;
    while (true)
    {
      nextChar = this.Peek();
      if (nextChar == END_OF_INPUT)
        break;

      if (sPosition == s.Length)
        break;

      var s1 = ((Char) nextChar).ToString();
      var s2 = s[sPosition].ToString();
      var comparison = String.Equals(s1, s2, this._stringComparison);
      if (!comparison)
        break;

      sPosition++;

      this.Read();
    }

    var success = sPosition == s.Length;
    if (success)
      this.AcceptNewPosition();
    else
      this.GoBackToSavedPosition();

    return success;
  }

  private static Boolean IsLineEnding(Char c) => ((c == '\n') || (c == '\r'));

  /// <summary>
  /// Starting from the scanner's current position, skip one or more line ending characters (carriage return (\r) or line feed (\n)).
  /// <para>Logically equivalent to the LINQ expression "s.TakeWhile(c => (c == '\n') || (c == '\r'))".</para>
  /// </summary>
  public void SkipLineEndings() => this.MatchWhile(IsLineEnding);

  /// <summary>
  /// Starting from the scanner's current position, skip one or more whitespace characters (but not
  /// line ending characters (carriage return (\r) or line feed (\n))).
  /// <para>Logically equivalent to the LINQ expression "s.TakeWhile(c => Char.IsWhiteSpace(c) &amp;&amp; (c != '\n') &amp;&amp; (c != '\r'))".</para>
  /// </summary>
  public void SkipLinearWhitespace() => this.MatchWhile(c => !IsLineEnding(c) && Char.IsWhiteSpace(c));

  /// <summary>
  /// Skips all whitespace, regardless of type. Combination of <see cref="SkipLineEndings"/> and <see cref="SkipLinearWhitespace"/>.
  /// </summary>
  public void SkipWhitespace() => this.MatchWhile(Char.IsWhiteSpace);

  /// <summary>
  /// Push the scanner's current position onto an internal stack.
  /// <para>This is useful when more than one match operation is required.  If one of the matches
  /// fails, the scanner's orignal position can be restored by calling <see cref="GoBackToSavedPosition"/>.
  /// If all of the matches succeed, call <see cref="AcceptNewPosition"/>.</para>
  /// <para>
  /// <b>Example:</b>
  /// <code>
  /// StringScanner scanner = new("abc");<br/>
  /// scanner.SavePosition();<br/>
  /// if (scanner.Match("a") &amp;&amp; scanner.Match("b") &amp;&amp; scanner.Match("c"))<br/>
  /// scanner.AcceptNewPosition();<br/>
  /// else<br/>
  /// scanner.GoBackToSavedPosition();<br/>
  /// </code>
  /// </para>
  /// </summary>
  public void SavePosition() => this._positions.Push((this._index, this._line, this._column));

  /// <summary>
  /// Reset's the scanner's internal position to the most recently saved position.
  /// If there are no saved positions, an exception is thrown.
  /// <para>See <see cref="SavePosition"/> for details.</para>
  /// </summary>
  public void GoBackToSavedPosition()
  {
    if (this._positions.TryPop(out var position))
    {
      this._index = position.Position;
      this._line = position.Line;
      this._column = position.Column;
    }
    else
    {
      throw new Exception(Properties.Resources.StringScanner_NoSavedPositionToGoBackTo);
    }
  }

  /// <summary>
  /// Discards the most recently saved position, leaving the scanner's internal position unchanged.
  /// If there are no saved positions, an exception is thrown.
  /// <para>See <see cref="SavePosition"/> for details.</para>
  /// </summary>
  public void AcceptNewPosition()
  {
    if (!this._positions.TryPop(out _))
      throw new Exception(Properties.Resources.StringScanner_NoSavedPosition);
  }
}

