/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lazy8.Core;

public static class DateTimeUtils
{
  /// <summary>
  /// Return the 1-based week number within the given <see cref="DateTime"/>'s month.
  /// Assumes that Sunday is the start of the week.
  /// <para>For example, April 2, 2017 falls in the second week of that month, so this method
  /// will return 2 for that date.</para>
  /// </summary>
  /// <param name="date">A <see cref="DateTime"/>.</param>
  /// <returns>A 1-based integer.</returns>
  public static Int32 WeekNumberInMonth(this DateTime date)
  {
    static Int32 getWeekNumberOfYear(DateTime d) =>
      CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);

    var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
    var weekNumberOffset = getWeekNumberOfYear(firstDayOfMonth);
    return (getWeekNumberOfYear(date) - weekNumberOffset) + 1;
  }

  /// <summary>
  /// Given a <see cref="DateTime"/> value, return an <see cref="Int32"/> indicating what calendar quarter the DateTime occurs in (1, 2, 3, or 4).
  /// </summary>
  /// <param name="dateTime">A <see cref="DateTime"/> value.</param>
  /// <returns>An <see cref="Int32"/> indicating what calendar quarter the DateTime occurs in (1, 2, 3, or 4).</returns>
  public static Int32 Quarter(this DateTime dateTime) => ((dateTime.Month + 2) / 3);

  /// <summary>
  /// Adds a quarter (i.e. three months) to <paramref name="dateTime"/> if <paramref name="quarters"/> is positive.
  /// If <paramref name="quarters"/> is negative, three months are subtracted from <paramref name="dateTime"/>.
  /// </summary>
  /// <param name="dateTime">A <see cref="DateTime"/> value.</param>
  /// <param name="quarters">A <see cref="Int32"/>.  May be negative.</param>
  /// <returns>A new <see cref="DateTime"/>.</returns>
  public static DateTime AddQuarters(this DateTime dateTime, Int32 quarters) => dateTime.AddMonths(quarters * 3);

  /// <summary>
  /// Throws an <see cref="ArgumentException"/> if <paramref name="quarter"/> is less than 1 or greater than 4.
  /// </summary>
  /// <param name="quarter">An <see cref="Int32"/> value.</param>
  private static void CheckQuarter(Int32 quarter)
  {
    if ((quarter < 1) || (quarter > 4))
      throw new ArgumentException(String.Format(Properties.Resources.DateTimeUtils_QuarterNotInRange, quarter));
  }

  /// <summary>
  /// Returns a <see cref="DateTime"/> containing the first day for the given <paramref name="quarter"/> in the given  <paramref name="year"/>.
  /// </summary>
  /// <param name="year">An <see cref="Int32"/> value.</param>
  /// <param name="quarter">An <see cref="Int32"/> value.</param>
  /// <returns>A <see cref="DateTime"/> value.</returns>
  public static DateTime GetFirstDayOfQuarter(Int32 year, Int32 quarter)
  {
    CheckQuarter(quarter);
    return (new DateTime(year, 1, 1)).AddQuarters(quarter - 1);
  }

  /// <summary>
  /// Returns a <see cref="DateTime"/> containing the first day of the quarter the given <paramref name="dateTime"/> falls in.
  /// </summary>
  /// <param name="dateTime">A <see cref="DateTime"/> value.</param>
  /// <returns>A new <see cref="DateTime"/>.</returns>
  public static DateTime FirstDayOfQuarter(this DateTime dateTime) => (new DateTime(dateTime.Year, 1, 1)).AddQuarters(dateTime.Quarter() - 1);

  /// <summary>
  /// Returns a <see cref="DateTime"/> containing the last day for the given <paramref name="quarter"/> in the given  <paramref name="year"/>.
  /// </summary>
  /// <param name="year">An <see cref="Int32"/> value.</param>
  /// <param name="quarter">An <see cref="Int32"/> value.</param>
  /// <returns>A <see cref="DateTime"/> value.</returns>
  public static DateTime GetLastDayOfQuarter(Int32 year, Int32 quarter)
  {
    CheckQuarter(quarter);
    return (new DateTime(year, 1, 1)).AddQuarters(quarter).AddDays(-1);
  }

  /// <summary>
  /// Returns a <see cref="DateTime"/> containing the last day of the quarter the given <paramref name="dateTime"/> falls in.
  /// </summary>
  /// <param name="dateTime">A <see cref="DateTime"/> value.</param>
  /// <returns>A new <see cref="DateTime"/>.</returns>
  public static DateTime LastDayOfQuarter(this DateTime dateTime) => (new DateTime(dateTime.Year, 1, 1)).AddQuarters(dateTime.Quarter()).AddDays(-1);

  /// <summary>
  /// Returns a <see cref="Boolean"/> indicating if <paramref name="dateTime1"/> and <paramref name="dateTime2"/> both fall in the same year and quarter.
  /// </summary>
  /// <param name="dateTime1">A <see cref="DateTime"/> value.</param>
  /// <param name="dateTime2">A <see cref="DateTime"/> value.</param>
  /// <returns>A <see cref="Boolean"/> value.</returns>
  public static Boolean AreDatesInSameYearAndQuarter(DateTime dateTime1, DateTime dateTime2) => (dateTime1.FirstDayOfQuarter() == dateTime2.FirstDayOfQuarter());

  /// <summary>
  /// Returns a <see cref="Boolean"/> indicating if <paramref name="dateTime"/> falls within the given <paramref name="year"/> and <paramref name="quarter"/>.
  /// </summary>
  /// <param name="dateTime">A <see cref="DateTime"/> value.</param>
  /// <param name="year">An <see cref="Int32"/> value.</param>
  /// <param name="quarter">An <see cref="Int32"/> value.</param>
  /// <returns></returns>
  public static Boolean IsDateInYearAndQuarter(DateTime dateTime, Int32 year, Int32 quarter)
  {
    CheckQuarter(quarter);
    return ((dateTime.Year == year) && (dateTime.Quarter() == quarter));
  }

  /// <summary>
  /// Analagous to the <see cref="System.Math.Min"/> method.  Given two <see cref="DateTime"/> values,
  /// return the smaller (earlier) of the two.
  /// </summary>
  /// <param name="dateTime1">A <see cref="DateTime"/> value.</param>
  /// <param name="dateTime2">A <see cref="DateTime"/> value.</param>
  /// <returns>The smaller (earlier) <see cref="DateTime"/> value.</returns>
  public static DateTime Min(DateTime dateTime1, DateTime dateTime2) => (dateTime1 < dateTime2) ? dateTime1 : dateTime2;

  /// <summary>
  /// Analagous to the <see cref="System.Math.Max"/> method.  Given two <see cref="DateTime"/> values,
  /// return the larger (later) of the two.
  /// </summary>
  /// <param name="dateTime1">A <see cref="DateTime"/> value.</param>
  /// <param name="dateTime2">A <see cref="DateTime"/> value.</param>
  /// <returns>The larger (later) <see cref="DateTime"/> value.</returns>
  public static DateTime Max(DateTime dateTime1, DateTime dateTime2) => (dateTime1 > dateTime2) ? dateTime1 : dateTime2;

  /// <summary>
  /// Return an <see cref="IEnumerable&lt;DateTime&gt;"/>
  /// containing a list of all DateTimes between (and including) <paramref name="startDateTime"/> and <paramref name="endDateTime"/>.
  /// <para>If <paramref name="startDateTime"/> is earlier than <paramref name="endDateTime"/>, then the result will be in ascending order.
  /// If the opposite is true, the result set will be in descending order.  If <paramref name="startDateTime"/> and <paramref name="endDateTime"/>
  /// have the same value, the result will contain one <see cref="DateTime"/> set to that value.</para>
  /// <para>For example:</para>
  /// <code>
  /// var time1 = new DateTime(2000, 1, 1);<br/>
  /// var time2 = new DateTime(2000, 1, 3);<br/>
  ///<br/>
  /// // Calling time1.To(time2) will return a list<br/>
  /// // of DateTimes in ascending order:<br/>
  ///<br/>
  /// 1/1/2000<br/>
  /// 1/2/2000<br/>
  /// 1/3/2000<br/>
  ///<br/>
  /// // The reverse call to time2.To(time1) will<br/>
  /// // return a descending list of DateTimes:<br/>
  ///<br/>
  /// 1/3/2000<br/>
  /// 1/2/2000<br/>
  /// 1/1/2000
  /// </code>
  /// </summary>
  /// <param name="dateTime1">A <see cref="DateTime"/> value.</param>
  /// <param name="dateTime2">A <see cref="DateTime"/> value.</param>
  /// <returns>An <see cref="IEnumerable&lt;DateTime&gt;"/> of <see cref="DateTime"/> values.</returns>
  public static IEnumerable<DateTime> To(this DateTime startDateTime, DateTime endDateTime)
  {
    var range = Enumerable.Range(0, Math.Abs((endDateTime - startDateTime).Days) + 1);

    return
      (startDateTime > endDateTime)
      ? range.Reverse().Select(n => endDateTime.AddDays(n))
      : range.Select(n => startDateTime.AddDays(n));
  }

  /// <summary>
  /// Given a date, specify the nthDay and dayOfWeek parameters to determine if date is the 
  /// "nthDay dayOfWeek" of date's month.  For example, is date the second Wednesday of date's month
  /// (nthDay == 2, dayOfWeek = DayOfWeek.Wednesday)?
  /// </summary>
  /// <param name="date">A <see cref="System.DateTime"/>.</param>
  /// <param name="nthDay">A <see cref="System.Int32"/> indicating the day's ordinal position w/i date's month.</param>
  /// <param name="dayOfWeek">A <see cref="System.DayOfWeek"/> indicating the day of the week.</param>
  /// <returns>A <see cref="System.Boolean"/> indicating if the supplied date is the nth "day name" for date's month.
  /// E.g. is date the third Tuesday (nthDay == 3, dayOfWeek == DayOfWeek.Tuesday) of date's month?</returns>
  public static Boolean IsNthDayOfMonth(this DateTime date, Int32 nthDay, DayOfWeek dayOfWeek)
  {
    /* Visualize a month as a series of 7-day blocks.  These blocks are not formal weeks.
       I.e. days 1 thru 7 are in block 1, days 8 thru 14 are in block 2, and so on.

       Simple division indicates which 7-day block a day falls in.*/

    var daysPositionInSevenDayBlocks = (Int32) Math.Ceiling(date.Day / 7.0d);

    /* Combining the day's position w/i a 7-day block, and the provided
       dayOfWeek, results in a boolean indicating where dayOfWeek falls
       in the month (e.g. first Monday, third Friday, etc.) */
    return (nthDay == daysPositionInSevenDayBlocks) && (date.DayOfWeek == dayOfWeek);
  }

  /// <summary>
  /// Return a <see cref="System.Boolean"/> indicating if the given date is a U.S. Federal holiday.
  /// <para>The return value is accurate for dates after 1978.  The legislative history of
  /// both Washington's Birthday and Veterans Day is somewhat muddled, resulting in these holidays being
  /// observed on several different dates in years prior to 1978.</para>
  /// <para>Also note that Columbus Day may be altered or disappear altogether at some point after this code was written (c. 2021).</para>
  /// </summary>
  /// <param name="date">A <see cref="System.DateTime"/>.</param>
  /// <returns>A <see cref="System.Boolean"/>.</returns>
  public static Boolean IsUSFederalHoliday(this DateTime date)
  {
    /* U.S. federal holidays are defined by law at https://www.law.cornell.edu/uscode/text/5/6103. */

    var isMonday = date.DayOfWeek == DayOfWeek.Monday;
    //var isThursday = date.DayOfWeek == DayOfWeek.Thursday;

    return
      ((date.Month == 1) && (date.Day == 1)) ||                                                  // January 1
      ((date.Month == 1) && date.IsNthDayOfMonth(3, DayOfWeek.Monday) && (date.Year >= 1986)) || // MLK Day
      ((date.Month == 2) && date.IsNthDayOfMonth(3, DayOfWeek.Monday)) ||                        // Washington's Birthday
      ((date.Month == 5) && isMonday && (date.AddDays(7).Month == 6)) ||                         // Memorial Day
      ((date.Month == 6) && (date.Day == 19) && (date.Year >= 2021)) ||                          // Juneteenth National Independence Day
      ((date.Month == 7) && (date.Day == 4)) ||                                                  // Independence Day
      ((date.Month == 9) && date.IsNthDayOfMonth(1, DayOfWeek.Monday)) ||                        // Labor Day
      ((date.Month == 10) && date.IsNthDayOfMonth(2, DayOfWeek.Monday)) ||                       // Columbus Day
      ((date.Month == 11) && (date.Day == 11)) ||                                                // Veterans Day
      ((date.Month == 11) && date.IsNthDayOfMonth(4, DayOfWeek.Thursday)) ||                     // Thanksgiving Day
      ((date.Month == 12) && (date.Day == 25));                                                  // Christmas Day
  }

  /// <summary>
  /// If a U.S. Federal holiday falls on a Saturday, the holiday is observed
  /// on the preceding Friday.  Likewise, if the holiday falls on a Sunday, the holiday
  /// is observed on the following Monday.
  /// </summary>
  /// <param name="date">A <see cref="System.DateTime"/>.</param>
  /// <returns>A <see cref="System.Boolean"/>.</returns>
  public static Boolean IsObservedUSFederalHoliday(this DateTime date)
  {
    var isFriday = date.DayOfWeek == DayOfWeek.Friday;
    var isMonday = date.DayOfWeek == DayOfWeek.Monday;

    return
      (isFriday && date.AddDays(1).IsUSFederalHoliday()) ||
      (isMonday && date.AddDays(-1).IsUSFederalHoliday());
  }

  /// <summary>
  /// Indicates if the given date parameter falls on a weekend.
  /// </summary>
  /// <param name="date">A <see cref="System.DateTime"/>.</param>
  /// <returns>A <see cref="System.Boolean"/>.</returns>
  public static Boolean IsWeekend(this DateTime date) => (date.DayOfWeek == DayOfWeek.Saturday) || (date.DayOfWeek == DayOfWeek.Sunday);
}

