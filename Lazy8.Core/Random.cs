/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Text;

namespace Lazy8.Core
{
  public enum LetterCaseMix
  {
    AllUpperCase,
    AllLowerCase,
    MixUpperCaseAndLowerCase
  };

  public static class RandomUtils
  {
    private static readonly Random _random = new Random();
    private static readonly Object _semaphore = new Object(); /* System.Random instance methods are not thread safe. */

    /// <summary>
    /// Returns true approximately 50% of the time, false the other 50% percent.
    /// </summary>
    /// <returns>A <see cref="Boolean"/> value.</returns>
    public static Boolean GetCoinFlip()
    {
      lock (_semaphore)
        return (_random.Next(2) == 0);
    }

    /// <summary>
    /// Returns true approximately <paramref name="probability"/>% of the time, false the other (1 - <paramref name="probability"/>)% percent.
    /// <paramref name="probability"/> must be between 0 and 1.0, inclusive.
    /// </summary>
    /// <param name="probability">A <see cref="Double"/> between 0 and 1.0, inclusive.</param>
    /// <returns>A <see cref="Boolean"/> value.</returns>
    public static Boolean GetCoinFlip(Double probability)
    {
      if ((probability <= 0.0) || (probability >= 1.0))
        throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Random_ProbabilityNotInRange, probability));
      else
        lock (_semaphore)
          return (_random.NextDouble() < probability);
    }

    /// <summary>
    /// Return a random <see cref="Byte"/> between <paramref name="low"/> and <paramref name="high"/>.
    /// </summary>
    /// <param name="low">A <see cref="Byte"/> value.</param>
    /// <param name="high">A <see cref="Byte"/> value.</param>
    /// <returns>A <see cref="Byte"/> value.</returns>
    public static Byte GetRandomByte(Byte low, Byte high) => (Byte) GetRandomInt64(low, high);

    /// <summary>
    /// Return a random <see cref="Int16"/> between <paramref name="low"/> and <paramref name="high"/>.
    /// </summary>
    /// <param name="low">An <see cref="Int16"/> value.</param>
    /// <param name="high">An <see cref="Int16"/> value.</param>
    /// <returns>An <see cref="Int16"/> value.</returns>
    public static Int16 GetRandomInt16(Int16 low, Int16 high) => (Int16) GetRandomInt64(low, high);

    /// <summary>
    /// Return a random <see cref="Int32"/> between <paramref name="low"/> and <paramref name="high"/>.
    /// </summary>
    /// <param name="low">An <see cref="Int32"/> value.</param>
    /// <param name="high">An <see cref="Int32"/> value.</param>
    /// <returns>An <see cref="Int32"/> value.</returns>
    public static Int32 GetRandomInt32(Int32 low, Int32 high) => (Int32) GetRandomInt64(low, high);

    /// <summary>
    /// Return a random <see cref="Int64"/> between <paramref name="low"/> and <paramref name="high"/>.
    /// </summary>
    /// <param name="low">An <see cref="Int64"/> value.</param>
    /// <param name="high">An <see cref="Int64"/> value.</param>
    /// <returns>An <see cref="Int64"/> value.</returns>
    public static Int64 GetRandomInt64(Int64 low, Int64 high) => (Int64) GetRandomDouble(low, high);

    /// <summary>
    /// Return a random <see cref="Double"/> between <paramref name="low"/> and <paramref name="high"/>.
    /// </summary>
    /// <param name="low">A <see cref="Double"/> value.</param>
    /// <param name="high">A <see cref="Double"/> value.</param>
    /// <returns>A <see cref="Double"/> value.</returns>
    public static Double GetRandomDouble(Double low, Double high)
    {
      if (low >= high)
        throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Random_LowGreaterThanHigh, low, high));
      else
        lock (_semaphore)
          return (low + (_random.NextDouble() * (high - low)));
    }

    /// <summary>
    /// Return a random <see cref="DateTime"/> between <paramref name="lowDate"/> and <paramref name="highDate"/>.
    /// </summary>
    /// <param name="lowDate">A <see cref="DateTime"/> value.</param>
    /// <param name="highDate">A <see cref="DateTime"/> value.</param>
    /// <returns>A <see cref="DateTime"/> value.</returns>
    public static DateTime GetRandomDateTime(DateTime lowDate, DateTime highDate)
    {
      if (lowDate > highDate)
        throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Random_LowDateGreaterThanHighDate, lowDate, highDate));
      else
        return new DateTime(GetRandomInt64(lowDate.Ticks, highDate.Ticks));
    }

    /// <summary>
    /// Return a random <see cref="TimeSpan"/> between <paramref name="lowTimeSpan"/> and <paramref name="highTimeSpan"/>.
    /// </summary>
    /// <param name="lowTimeSpan">A <see cref="TimeSpan"/> value.</param>
    /// <param name="highTimeSpan">A <see cref="TimeSpan"/> value.</param>
    /// <returns>A <see cref="TimeSpan"/> value.</returns>
    public static TimeSpan GetRandomTimeSpan(TimeSpan lowTimeSpan, TimeSpan highTimeSpan)
    {
      if (lowTimeSpan > highTimeSpan)
        throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Random_LowTimeSpanGreaterThanHighDate, lowTimeSpan, highTimeSpan));
      else
        return new TimeSpan(GetRandomInt64(lowTimeSpan.Ticks, highTimeSpan.Ticks));
    }

    /// <summary>
    /// Return a random <see cref="String"/> of length <paramref name="count"/> consisting of characters between
    /// <paramref name="lowChar"/> and <paramref name="highChar"/>.  Set <paramref name="letterCaseMix"/> to
    /// indicate whether the return string consists of all upper case letters, all lower case letters, or a mix
    /// of upper and lower case letters.
    /// </summary>
    /// <param name="lowChar">A <see cref="Char"/> value.</param>
    /// <param name="highChar">A <see cref="Char"/> value.</param>
    /// <param name="count">An <see cref="Int32"/> value.</param>
    /// <param name="letterCaseMix">A <see cref="LetterCaseMix"/> value.</param>
    /// <returns>A <see cref="String"/> value.</returns>
    public static String GetRandomString(Char lowChar, Char highChar, Int32 count, LetterCaseMix letterCaseMix)
    {
      if (lowChar >= highChar)
        throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Random_LowCharGreaterThanHighChar, lowChar, highChar));

      if (count < 1)
        throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Random_CountOutOfRange, count));

      var result = new StringBuilder(count);
      Char randomChar;

      for (Int32 idx = 0; idx < count; idx++)
      {
        lock (_semaphore)
          randomChar = (Char) _random.Next(lowChar, highChar + 1);

        switch (letterCaseMix)
        {
          case LetterCaseMix.AllUpperCase:
            result.Append(Char.ToUpper(randomChar));
            break;

          case LetterCaseMix.AllLowerCase:
            result.Append(Char.ToLower(randomChar));
            break;

          case LetterCaseMix.MixUpperCaseAndLowerCase:
            if (GetCoinFlip())
              result.Append(Char.ToUpper(randomChar));
            else
              result.Append(Char.ToLower(randomChar));
            break;

          default:
            throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.Random_UnknownLetterCaseMixValue, letterCaseMix));
        }
      }

      return result.ToString();
    }
  }
}
