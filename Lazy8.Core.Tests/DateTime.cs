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
    public void WeekNumberOfMonthTest()
    {
      /* April 2017 is spread over 6 numerical weeks. */

      Assert.That(new DateTime(2017, 4, 1).WeekNumberOfMonth(), Is.EqualTo(1));
      Assert.That(new DateTime(2017, 4, 2).WeekNumberOfMonth(), Is.EqualTo(2));
      Assert.That(new DateTime(2017, 4, 9).WeekNumberOfMonth(), Is.EqualTo(3));
      Assert.That(new DateTime(2017, 4, 16).WeekNumberOfMonth(), Is.EqualTo(4));
      Assert.That(new DateTime(2017, 4, 23).WeekNumberOfMonth(), Is.EqualTo(5));
      Assert.That(new DateTime(2017, 4, 30).WeekNumberOfMonth(), Is.EqualTo(6));

      /* Like the vast majority of months, September 2021 has 5 numerical weeks. */

      Assert.That(new DateTime(2021, 9, 1).WeekNumberOfMonth(), Is.EqualTo(1));
      Assert.That(new DateTime(2021, 9, 8).WeekNumberOfMonth(), Is.EqualTo(2));
      Assert.That(new DateTime(2021, 9, 15).WeekNumberOfMonth(), Is.EqualTo(3));
      Assert.That(new DateTime(2021, 9, 22).WeekNumberOfMonth(), Is.EqualTo(4));
      Assert.That(new DateTime(2021, 9, 29).WeekNumberOfMonth(), Is.EqualTo(5));

      /* February occasionally starts on a Sunday (e.g. 2015), and in that case has 4 numerical weeks. */

      Assert.That(new DateTime(2015, 2, 1).WeekNumberOfMonth(), Is.EqualTo(1));
      Assert.That(new DateTime(2015, 2, 8).WeekNumberOfMonth(), Is.EqualTo(2));
      Assert.That(new DateTime(2015, 2, 15).WeekNumberOfMonth(), Is.EqualTo(3));
      Assert.That(new DateTime(2015, 2, 28).WeekNumberOfMonth(), Is.EqualTo(4));
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
      var range = startDateTime.To(endDateTime);

      /* A start date that is the same as the end date should result
         in a list of days with one DateTime value, and that value should be
         the same as the start and end dates. */
      Assert.That(range.Count(), Is.EqualTo(1));
      Assert.That(range.First(), Is.EqualTo(startDateTime));

      //////////////////////////////////////////////////////////////////////////////////

      endDateTime = new DateTime(2000, 1, 3);
      range = startDateTime.To(endDateTime);

      /* A start date that is earlier than the end date should result
         in a list of DateTimes that fall between those two dates, inclusive. */
      Assert.That(range.ToArray(), Is.EqualTo(new[] { new DateTime(2000, 1, 1), new DateTime(2000, 1, 2), new DateTime(2000, 1, 3) } ));

      //////////////////////////////////////////////////////////////////////////////////

      startDateTime = new DateTime(2000, 1, 3);
      endDateTime = new DateTime(2000, 1, 1);
      range = startDateTime.To(endDateTime);

      /* A start date that is later than the end date should result
         in a list of DateTimes that fall between those two dates, inclusive,
         but in descending order. */
      Assert.That(range.ToArray(), Is.EqualTo(new[] { new DateTime(2000, 1, 3), new DateTime(2000, 1, 2), new DateTime(2000, 1, 1) }));
    }
  }
}
