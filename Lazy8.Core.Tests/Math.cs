/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Globalization;

using NUnit.Framework;

namespace Lazy8.Core.Tests;

[TestFixture]
public class MathUtilsTests
{
  [Test]
  public void IsIntegerTest()
  {
    Assert.That("".IsInteger(), Is.False);
    Assert.That("a".IsInteger(), Is.False);

    Assert.That("1".IsInteger(), Is.True);
    Assert.That("1.0".IsInteger(), Is.False);

    Assert.That(Int64.MaxValue.ToString().IsInteger(), Is.True);
    Assert.That(Int64.MinValue.ToString().IsInteger(), Is.True);

    /* Yes, these three tests look weird.

       The positive infinity, negative inifinity, and special NaN (not a number) symbols
       usually aren't very useful as numbers.  But as far as .Net Core is concerned,
       they are all technically numeric values.  This means they qualify as System.Int64 values. */

    Assert.That(NumberFormatInfo.CurrentInfo.PositiveInfinitySymbol.IsInteger(), Is.True);
    Assert.That(NumberFormatInfo.CurrentInfo.NegativeInfinitySymbol.IsInteger(), Is.True);
    Assert.That(NumberFormatInfo.CurrentInfo.NaNSymbol.IsInteger(), Is.True);
  }

  [Test]
  public void IsDoubleTest()
  {
    Assert.That("".IsDouble(), Is.False);
    Assert.That("a".IsDouble(), Is.False);

    Assert.That("1".IsDouble(), Is.True);
    Assert.That("1.0".IsDouble(), Is.True);

    Assert.That(Double.MaxValue.ToString().IsDouble(), Is.True);
    Assert.That(Double.MinValue.ToString().IsDouble(), Is.True);

    /* See IsIntegerTest() above. */

    Assert.That(NumberFormatInfo.CurrentInfo.PositiveInfinitySymbol.IsDouble(), Is.True);
    Assert.That(NumberFormatInfo.CurrentInfo.NegativeInfinitySymbol.IsDouble(), Is.True);
    Assert.That(NumberFormatInfo.CurrentInfo.NaNSymbol.IsDouble(), Is.True);
  }

  [Test]
  public void ToBaseTest()
  {
    Assert.That("1", Is.EqualTo(1.ToBase(16)));

    Assert.That("1010", Is.EqualTo(10.ToBase(2)));
    Assert.That("12", Is.EqualTo(10.ToBase(8)));
    Assert.That("A", Is.EqualTo(10.ToBase(16)));
  }

  [Test]
  public void FromBaseTest()
  {
    Assert.That(1, Is.EqualTo("1".FromBase(2)));

    Assert.That(2, Is.EqualTo("10".FromBase(2)));
    Assert.That(10, Is.EqualTo("1010".FromBase(2)));

    Assert.That(10, Is.EqualTo("12".FromBase(8)));
    Assert.That(10, Is.EqualTo("A".FromBase(16)));
  }

  [Test]
  public void IsInRangeTest()
  {
    const Int32 LOW = 5;
    const Int32 HIGH = 9;
    const Int32 IN_RANGE = 7;
    const Int32 OUT_OF_RANGE_LOW = 1;
    const Int32 OUT_OF_RANGE_HIGH = 10;

    Assert.That(IN_RANGE.IsInRange(LOW, HIGH), Is.True);

    Assert.That(OUT_OF_RANGE_LOW.IsInRange(LOW, HIGH), Is.False);
    Assert.That(OUT_OF_RANGE_HIGH.IsInRange(LOW, HIGH), Is.False);

    Assert.That(LOW.IsInRange(LOW, HIGH, RangeCheck.Inclusive), Is.True);
    Assert.That(LOW.IsInRange(LOW, HIGH, RangeCheck.Exclusive), Is.False);

    Assert.That(HIGH.IsInRange(LOW, HIGH, RangeCheck.Inclusive), Is.True);
    Assert.That(HIGH.IsInRange(LOW, HIGH, RangeCheck.Exclusive), Is.False);
  }

  [Test]
  public void SafeConvertSingleToDecimalTest()
  {
    Assert.That(Single.MinValue.SafeConvertToDecimal(), Is.EqualTo(Convert.ToSingle(Decimal.MinValue)));
    Assert.That((Convert.ToSingle(Decimal.MinValue) - 1).SafeConvertToDecimal(), Is.EqualTo(Convert.ToSingle(Decimal.MinValue)));
    Assert.That(Convert.ToSingle(Decimal.MinValue).SafeConvertToDecimal(), Is.EqualTo(Convert.ToSingle(Decimal.MinValue)));
    Assert.That((Convert.ToSingle(Decimal.MinValue) + 1).SafeConvertToDecimal(), Is.EqualTo(Convert.ToSingle(Decimal.MinValue) + 1));

    Assert.That(Convert.ToSingle(69).SafeConvertToDecimal(), Is.EqualTo(69.0));

    Assert.That((Convert.ToSingle(Decimal.MaxValue) - 1).SafeConvertToDecimal(), Is.EqualTo(Convert.ToSingle(Decimal.MaxValue) - 1));
    Assert.That(Convert.ToSingle(Decimal.MaxValue).SafeConvertToDecimal(), Is.EqualTo(Convert.ToSingle(Decimal.MaxValue)));
    Assert.That((Convert.ToSingle(Decimal.MaxValue) + 1).SafeConvertToDecimal(), Is.EqualTo(Convert.ToSingle(Decimal.MaxValue)));
    Assert.That(Single.MaxValue.SafeConvertToDecimal(), Is.EqualTo(Convert.ToSingle(Decimal.MaxValue)));
  }

  [Test]
  public void SafeConvertDoubleToDecimalTest()
  {
    Assert.That(Double.MinValue.SafeConvertToDecimal(), Is.EqualTo(Convert.ToSingle(Decimal.MinValue)));
    Assert.That((Convert.ToDouble(Decimal.MinValue) - 1).SafeConvertToDecimal(), Is.EqualTo(Convert.ToDouble(Decimal.MinValue)));
    Assert.That(Convert.ToDouble(Decimal.MinValue).SafeConvertToDecimal(), Is.EqualTo(Convert.ToDouble(Decimal.MinValue)));
    Assert.That((Convert.ToDouble(Decimal.MinValue) + 1).SafeConvertToDecimal(), Is.EqualTo(Convert.ToDouble(Decimal.MinValue) + 1));

    Assert.That(Convert.ToDouble(69).SafeConvertToDecimal(), Is.EqualTo(69.0));

    Assert.That((Convert.ToDouble(Decimal.MaxValue) - 1).SafeConvertToDecimal(), Is.EqualTo(Convert.ToDouble(Decimal.MaxValue) - 1));
    Assert.That(Convert.ToDouble(Decimal.MaxValue).SafeConvertToDecimal(), Is.EqualTo(Convert.ToDouble(Decimal.MaxValue)));
    Assert.That((Convert.ToDouble(Decimal.MaxValue) + 1).SafeConvertToDecimal(), Is.EqualTo(Convert.ToDouble(Decimal.MaxValue)));
    Assert.That(Double.MaxValue.SafeConvertToDecimal(), Is.EqualTo(Convert.ToSingle(Decimal.MaxValue)));
  }
}

