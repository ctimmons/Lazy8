/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Linq;

using NUnit.Framework;

namespace Lazy8.Core.Tests;

[TestFixture]
public class DateTimeUtilsTests
{
  private readonly Tuple<DateTime, DateTime>[] _quarterDateRanges =
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

    Assert.That(new DateTime(2017, 4, 1).WeekNumberInMonth(), Is.EqualTo(1));
    Assert.That(new DateTime(2017, 4, 2).WeekNumberInMonth(), Is.EqualTo(2));
    Assert.That(new DateTime(2017, 4, 9).WeekNumberInMonth(), Is.EqualTo(3));
    Assert.That(new DateTime(2017, 4, 16).WeekNumberInMonth(), Is.EqualTo(4));
    Assert.That(new DateTime(2017, 4, 23).WeekNumberInMonth(), Is.EqualTo(5));
    Assert.That(new DateTime(2017, 4, 30).WeekNumberInMonth(), Is.EqualTo(6));

    /* Like the vast majority of months, September 2021 has 5 numerical weeks. */

    Assert.That(new DateTime(2021, 9, 1).WeekNumberInMonth(), Is.EqualTo(1));
    Assert.That(new DateTime(2021, 9, 8).WeekNumberInMonth(), Is.EqualTo(2));
    Assert.That(new DateTime(2021, 9, 15).WeekNumberInMonth(), Is.EqualTo(3));
    Assert.That(new DateTime(2021, 9, 22).WeekNumberInMonth(), Is.EqualTo(4));
    Assert.That(new DateTime(2021, 9, 29).WeekNumberInMonth(), Is.EqualTo(5));

    /* February occasionally starts on a Sunday (e.g. 2015), and in that case has 4 numerical weeks. */

    Assert.That(new DateTime(2015, 2, 1).WeekNumberInMonth(), Is.EqualTo(1));
    Assert.That(new DateTime(2015, 2, 8).WeekNumberInMonth(), Is.EqualTo(2));
    Assert.That(new DateTime(2015, 2, 15).WeekNumberInMonth(), Is.EqualTo(3));
    Assert.That(new DateTime(2015, 2, 28).WeekNumberInMonth(), Is.EqualTo(4));
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
    Assert.That(range.ToArray(), Is.EqualTo(new[] { new DateTime(2000, 1, 1), new DateTime(2000, 1, 2), new DateTime(2000, 1, 3) }));

    //////////////////////////////////////////////////////////////////////////////////

    startDateTime = new DateTime(2000, 1, 3);
    endDateTime = new DateTime(2000, 1, 1);
    range = startDateTime.To(endDateTime);

    /* A start date that is later than the end date should result
       in a list of DateTimes that fall between those two dates, inclusive,
       but in descending order. */
    Assert.That(range.ToArray(), Is.EqualTo(new[] { new DateTime(2000, 1, 3), new DateTime(2000, 1, 2), new DateTime(2000, 1, 1) }));
  }

  [Test]
  public void NthDayInMonthTest()
  {
    Assert.That(new DateTime(2021, 10, 1).NthDayInMonth(), Is.EqualTo(1)); // First Friday.
    Assert.That(new DateTime(2021, 10, 8).NthDayInMonth(), Is.EqualTo(2)); // Second Friday.
    Assert.That(new DateTime(2021, 10, 13).NthDayInMonth(), Is.EqualTo(2)); // Second Wednesday.
    Assert.That(new DateTime(2021, 10, 30).NthDayInMonth(), Is.EqualTo(5)); // Fifth Saturday.
  }

  [Test]
  public void IsUSFederalHolidayTest()
  {
    /* New Year's day. */
    Assert.That(new DateTime(2021, 1, 1).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 1, 2).IsUSFederalHoliday(), Is.EqualTo(false));

    /* MLK day (came into effect 1986). */
    Assert.That(new DateTime(1986, 1, 20).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(1985, 1, 21).IsUSFederalHoliday(), Is.EqualTo(false));

    /* Washington's Birthday. */
    Assert.That(new DateTime(2021, 2, 15).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 2, 16).IsUSFederalHoliday(), Is.EqualTo(false));

    /* Memorial Day */
    Assert.That(new DateTime(2021, 5, 31).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 5, 30).IsUSFederalHoliday(), Is.EqualTo(false));

    /* Juneteenth National Independence Day (came into effect 2021). */
    Assert.That(new DateTime(2021, 6, 19).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2020, 6, 19).IsUSFederalHoliday(), Is.EqualTo(false));

    /* Independence Day */
    Assert.That(new DateTime(2021, 7, 4).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 7, 5).IsUSFederalHoliday(), Is.EqualTo(false));

    /* Labor Day */
    Assert.That(new DateTime(2021, 9, 6).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 9, 7).IsUSFederalHoliday(), Is.EqualTo(false));

    /* Columbus Day */
    Assert.That(new DateTime(2021, 10, 11).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 10, 12).IsUSFederalHoliday(), Is.EqualTo(false));

    /* Veterans Day */
    Assert.That(new DateTime(2021, 11, 11).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 11, 12).IsUSFederalHoliday(), Is.EqualTo(false));

    /* Thanksgiving Day */
    Assert.That(new DateTime(2021, 11, 25).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 11, 26).IsUSFederalHoliday(), Is.EqualTo(false));

    /* Christmas Day */
    Assert.That(new DateTime(2021, 12, 25).IsUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 12, 26).IsUSFederalHoliday(), Is.EqualTo(false));
  }

  [Test]
  public void IsObservedUSFederalHolidayTest()
  {
    Assert.That(new DateTime(2021, 6, 18).IsObservedUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 6, 19).IsObservedUSFederalHoliday(), Is.EqualTo(false));

    Assert.That(new DateTime(2021, 7, 5).IsObservedUSFederalHoliday(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 7, 4).IsObservedUSFederalHoliday(), Is.EqualTo(false));
  }

  [Test]
  public void IsWeekendTest()
  {
    Assert.That(new DateTime(2021, 5, 7).IsWeekend(), Is.EqualTo(false));
    Assert.That(new DateTime(2021, 5, 8).IsWeekend(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 5, 9).IsWeekend(), Is.EqualTo(true));
    Assert.That(new DateTime(2021, 5, 10).IsWeekend(), Is.EqualTo(false));
  }
}

