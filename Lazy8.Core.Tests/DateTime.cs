/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Linq;

using NUnit.Framework;

namespace Lazy8.Core.Tests
{
  [TestFixture]
  public class DateTimeUtilsTests
  {
    private Tuple<DateTime, DateTime>[] _quarterDateRanges =
      new Tuple<DateTime, DateTime>[4]
      {
        new Tuple<DateTime, DateTime>(new DateTime(2000, 1, 1), new DateTime(2000, 3, 31)),
        new Tuple<DateTime, DateTime>(new DateTime(2000, 4, 1), new DateTime(2000, 6, 30)),
        new Tuple<DateTime, DateTime>(new DateTime(2000, 7, 1), new DateTime(2000, 9, 30)),
        new Tuple<DateTime, DateTime>(new DateTime(2000, 10, 1), new DateTime(2000, 12, 31))
      };

    private void RunActionOverQuarterDateRanges(Action<Int32, DateTime, DateTime, DateTime> action)
    {
      for (var quarter = 0; quarter < this._quarterDateRanges.Length; quarter++)
      {
        var quarterStartDate = this._quarterDateRanges[quarter].Item1;
        var quarterEndDate = this._quarterDateRanges[quarter].Item2;

        for (var date = quarterStartDate; date <= quarterEndDate; date = date.AddDays(1))
          action(quarter + 1, quarterStartDate, quarterEndDate, date);
      }
    }

    [Test]
    public void AddQuarterTest()
    {
      this.RunActionOverQuarterDateRanges(
        (quarter, quarterStartDate, quarterEndDate, date) =>
        {
          Assert.That((quarter % 4) + 1, Is.EqualTo(DateTimeUtils.AddQuarters(date, 1).Quarter()));
          Assert.That(((quarter + 1) % 4) + 1, Is.EqualTo(DateTimeUtils.AddQuarters(date, 2).Quarter()));
          Assert.That(((quarter + 2) % 4) + 1, Is.EqualTo(DateTimeUtils.AddQuarters(date, 3).Quarter()));
          Assert.That(((quarter + 3) % 4) + 1, Is.EqualTo(DateTimeUtils.AddQuarters(date, 4).Quarter()));
        });
    }

    [Test]
    public void AreDatesInSameYearAndQuarterTest()
    {
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.That(DateTimeUtils.AreDatesInSameYearAndQuarter(date, quarterStartDate), Is.True));
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.That(DateTimeUtils.AreDatesInSameYearAndQuarter(date, quarterStartDate.AddYears(-1)), Is.False));
    }

    [Test]
    public void GetFirstDayOfQuarterTest()
    {
      Assert.Throws<ArgumentException>(() => DateTimeUtils.GetFirstDayOfQuarter(2000, 0));
      Assert.Throws<ArgumentException>(() => DateTimeUtils.GetFirstDayOfQuarter(2000, 5));

      Assert.That(new DateTime(2000, 1, 1), Is.EqualTo(DateTimeUtils.GetFirstDayOfQuarter(2000, 1)));
      Assert.That(new DateTime(2000, 4, 1), Is.EqualTo(DateTimeUtils.GetFirstDayOfQuarter(2000, 2)));
      Assert.That(new DateTime(2000, 7, 1), Is.EqualTo(DateTimeUtils.GetFirstDayOfQuarter(2000, 3)));
      Assert.That(new DateTime(2000, 10, 1), Is.EqualTo(DateTimeUtils.GetFirstDayOfQuarter(2000, 4)));

      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.That(quarterStartDate, Is.EqualTo(date.FirstDayOfQuarter())));
    }

    [Test]
    public void GetLastDayOfQuarterTest()
    {
      Assert.Throws<ArgumentException>(() => DateTimeUtils.GetLastDayOfQuarter(2000, 0));
      Assert.Throws<ArgumentException>(() => DateTimeUtils.GetLastDayOfQuarter(2000, 5));

      Assert.That(new DateTime(2000, 3, 31), Is.EqualTo(DateTimeUtils.GetLastDayOfQuarter(2000, 1)));
      Assert.That(new DateTime(2000, 6, 30), Is.EqualTo(DateTimeUtils.GetLastDayOfQuarter(2000, 2)));
      Assert.That(new DateTime(2000, 9, 30), Is.EqualTo(DateTimeUtils.GetLastDayOfQuarter(2000, 3)));
      Assert.That(new DateTime(2000, 12, 31), Is.EqualTo(DateTimeUtils.GetLastDayOfQuarter(2000, 4)));

      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.That(quarterEndDate, Is.EqualTo(date.LastDayOfQuarter())));
    }

    [Test]
    public void GetQuarterTest()
    {
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.That(quarter, Is.EqualTo(date.Quarter())));
    }

    [Test]
    public void IsDateInYearAndQuarterTest()
    {
      Assert.Throws<ArgumentException>(() => DateTimeUtils.IsDateInYearAndQuarter(new DateTime(2000, 1, 1), 2000, 0));
      Assert.Throws<ArgumentException>(() => DateTimeUtils.IsDateInYearAndQuarter(new DateTime(2000, 1, 1), 2000, 5));

      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.That(DateTimeUtils.IsDateInYearAndQuarter(date, quarterStartDate.Year, quarter), Is.True));
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.That(DateTimeUtils.IsDateInYearAndQuarter(date, quarterStartDate.Year + 1, quarter), Is.False));
    }

    [Test]
    public void MaxTest()
    {
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.That(date.AddYears(1), Is.EqualTo(DateTimeUtils.Max(date.AddYears(1), quarterStartDate))));
    }

    [Test]
    public void MinTest()
    {
      this.RunActionOverQuarterDateRanges((quarter, quarterStartDate, quarterEndDate, date) => Assert.That(date, Is.EqualTo(DateTimeUtils.Min(date, quarterStartDate.AddYears(1)))));
    }

    [Test]
    public void ToTest()
    {
      var startDateTime = new DateTime(2000, 1, 1);
      var endDateTime = new DateTime(2000, 1, 1);
      var days = startDateTime.To(endDateTime);

      /* A start date that is the same as the end date should result
         in a list of days with one DateTime value, and that value should be
         the same as the start and end dates. */
      Assert.That(days.Count(), Is.EqualTo(1));
      Assert.That(days.First(), Is.EqualTo(startDateTime));

      //////////////////////////////////////////////////////////////////////////////////

      endDateTime = new DateTime(2000, 1, 10);
      days = startDateTime.To(endDateTime);

      /* A start date that is earlier than the end date should result
         in a list of DateTimes that fall between those two dates, inclusive. */
      Assert.That(days.Count(), Is.EqualTo(10));

      //////////////////////////////////////////////////////////////////////////////////

      startDateTime = new DateTime(2000, 1, 10);
      endDateTime = new DateTime(2000, 1, 1);
      days = startDateTime.To(endDateTime);

      /* A start date that is later than the end date should result
         in a list of DateTimes that fall between those two dates, inclusive,
         but in descending order. */
      Assert.That(days.Count(), Is.EqualTo(10));
    }
  }
}
