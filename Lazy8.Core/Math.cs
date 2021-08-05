/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Globalization;

namespace Lazy8.Core
{
  public enum RangeCheck { Exclusive, Inclusive }

  public static class MathUtils
  {
    /// <summary>
    /// Return true if <paramref name="number"/> contains an integer (with optional sign) that fits into a <see cref="Double"/>.  False otherwise.
    /// </summary>
    /// <param name="number">A <see cref="String"/>.</param>
    /// <returns>True if <paramref name="number"/> contains an integer (with optional sign) that fits into a <see cref="Double"/>.  False otherwise.</returns>
    public static Boolean IsInteger(this String number) =>
      /* Double.TryParse is used so num values with a larger range than Int64 can be handled. */
      Double.TryParse(number, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var _);

    /// <summary>
    /// Return true if <paramref name="number"/> contains a floating point value.  Leading sign, decimal point, and exponent are allowed.  Returns false otherwise.
    /// </summary>
    /// <param name="number">A <see cref="String"/>.</param>
    /// <returns>True if <paramref name="number"/> contains a floating point value.  Leading sign, decimal point, and exponent are allowed.  Returns false otherwise.</returns>
    public static Boolean IsDouble(this String number) => Double.TryParse(number, NumberStyles.Float, NumberFormatInfo.CurrentInfo, out var _);

    private static void CheckBase(Int32 @base)
    {
      if (!@base.IsInRange(2, 36))
        throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.MathUtils_BaseOutOfRange, @base));
    }

    /// <summary>
    /// Convert <paramref name="number"/> to the given <paramref name="base"/>, and return the result as a <see cref="String"/>.
    /// </summary>
    /// <param name="number">An <see cref="Int32"/> value.</param>
    /// <param name="base">An <see cref="Int32"/> between 2 and 36, inclusive.</param>
    /// <returns>A <see cref="String"/> containing the converted number.</returns>
    public static String ToBase(this Int32 number, Int32 toBase) => Convert.ToInt64(number).ToBase(toBase);

    /// <summary>
    /// Convert <paramref name="number"/> to the given <paramref name="base"/>, and return the result as a <see cref="String"/>.
    /// </summary>
    /// <param name="number">An <see cref="Int64"/> value.</param>
    /// <param name="base">An <see cref="Int32"/> between 2 and 36, inclusive.</param>
    /// <returns>A <see cref="String"/> containing the converted number.</returns>
    public static String ToBase(this Int64 number, Int32 @base)
    {
      CheckBase(@base);

      var digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(0, @base);
      var result = "";

      while (number > 0)
      {
        var digitValue = (Int32) (number % (Double) @base);
        number /= @base;
        result = digits.Substring(digitValue, 1) + result;
      }

      return result;
    }

    /// <summary>
    /// Given a <see cref="String"/> that contains a numeric value in <paramref name="base"/>, convert that value to an <see cref="Int32"/> and return it.
    /// </summary>
    /// <param name="number">A <see cref="String"/> value.</param>
    /// <param name="base">An <see cref="Int32"/> between 2 and 36, inclusive.</param>
    /// <returns>An <see cref="Int32"/> representing the numeric value in <paramref name="number"/>.</returns>
    public static Int32 FromBase(this String number, Int32 @base)
    {
      CheckBase(@base);

      var digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(0, @base);
      var result = 0;

      number = number.ToUpper();

      for (Int32 i = 0; i < number.Length; i++)
      {
        var digitValue = digits.IndexOf(number.Substring(i, 1), 0, digits.Length);

        if (digitValue < 0)
          throw new ArgumentException(String.Format(Properties.Resources.MathUtils_BadDigit, number, @base));

        result = (result * @base) + digitValue;
      }

      return result;
    }

    /// <summary>
    /// Given a <see cref="String"/> that contains a numeric value in <paramref name="fromBase"/>,
    /// convert that value to a <see cref="String"/> in <paramref name="toBase"/> and return it.
    /// </summary>
    /// <param name="number">A <see cref="String"/> containing a number in <paramref name="fromBase"/>.</param>
    /// <param name="fromBase">An <see cref="Int32"/> between 2 and 36, inclusive.</param>
    /// <param name="toBase">An <see cref="Int32"/> between 2 and 36, inclusive.</param>
    /// <returns>A <see cref="String"/> containing the converted number.</returns>
    public static String FromBaseToBase(this String number, Int32 fromBase, Int32 toBase) =>
      number.FromBase(fromBase).ToBase(toBase).TrimStart("0".ToCharArray());

    /// <summary>
    /// Return a <see cref="Boolean"/> indicating if <paramref name="value"/> is in between <paramref name="min"/> and <paramref name="max"/>.
    /// The range check is inclusive of min and max values.
    /// </summary>
    /// <param name="value">An <see cref="Int32"/> value.</param>
    /// <param name="min">An <see cref="Int32"/> value.  Must be less than <paramref name="max"/>.</param>
    /// <param name="max">An <see cref="Int32"/> value.  Must be greater than <paramref name="min"/>.</param>
    /// <param name="rangeCheck">A <see cref="RangeCheck"/> enumeration value.</param>
    /// <returns>A <see cref="Boolean"/> value.</returns>
    public static Boolean IsInRange(this Int32 value, Int32 min, Int32 max) => value.IsInRange(min, max, RangeCheck.Inclusive);

    /// <summary>
    /// Return a <see cref="Boolean"/> indicating if <paramref name="value"/> is in between <paramref name="min"/> and <paramref name="max"/>.
    /// The <paramref name="rangeCheck"/> parameter allows the caller to choose if the range check is inclusive or exclusive of the
    /// min and max values.
    /// </summary>
    /// <param name="value">An <see cref="Int32"/> value.</param>
    /// <param name="min">An <see cref="Int32"/> value.  Must be less than <paramref name="max"/>.</param>
    /// <param name="max">An <see cref="Int32"/> value.  Must be greater than <paramref name="min"/>.</param>
    /// <param name="rangeCheck">A <see cref="RangeCheck"/> enumeration value.</param>
    /// <returns>A <see cref="Boolean"/> value.</returns>
    public static Boolean IsInRange(this Int32 value, Int32 min, Int32 max, RangeCheck rangeCheck)
    {
      if (min > max)
        throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.MathUtils_MinGreaterThanMax, min, max));

      return 
        rangeCheck switch
        {
          RangeCheck.Exclusive => ((value > min) && (value < max)),
          RangeCheck.Inclusive => ((value >= min) && (value <= max)),
          _ => throw new ArgumentOutOfRangeException(String.Format(Properties.Resources.MathUtils_BadRangeCheckValue, rangeCheck)),
        };
    }
  }
}
