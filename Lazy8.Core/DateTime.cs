/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lazy8.Core
{
  public static class DateTimeUtils
  {
    /// <summary>
    /// Returns the week number for the given <see cref="DateTime"/>'s month.
    /// Assumes that Sunday is the start of the week.
    /// </summary>
    /// <param name="date">A <see cref="DateTime"/>.</param>
    /// <returns>A 1-based integer.</returns>
    public static Int32 WeekNumberOfMonth(this DateTime date)
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
      var signedNumberOfDays = (endDateTime - startDateTime).Days;
      var sign = (signedNumberOfDays < 0) ? -1 : 1;
      var absoluteNumberOfDays = Math.Abs(signedNumberOfDays) + 1; /* "+ 1" to ensure both the start and end dates are included in the result set. */
      var result = new List<DateTime>(absoluteNumberOfDays);

      for (var day = 0; day < absoluteNumberOfDays; day++)
        result.Add(startDateTime.AddDays(day * sign));

      return result;
    }
  }
}
