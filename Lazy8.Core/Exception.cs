﻿/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Lazy8.Core;

/// <summary>
/// Simplifies creating an Exception with a message created via String.Format().
/// <para>Instead of calling 'throw new Exception(String.Format("", ...))', this class's
/// constructor eliminates the need to call String.Format(), i.e. 'throw new ExceptionFmt("", ...)'.</para>
/// </summary>
public class ExceptionFmt(String message, params Object[] args) : Exception(String.Format(message, args))
{
}

/// <summary>
/// Simplifies creating an NotImplementedException with a message created via String.Format().
/// <para>Instead of calling 'throw new NotImplementedException(String.Format("", ...))', this class's
/// constructor eliminates the need to call String.Format(), i.e. 'throw new NotImplementedExceptionFmt("", ...)'.</para>
/// </summary>
public class ArgumentExceptionFmt(String message, params Object[] args) : ArgumentException(String.Format(message, args))
{
}

/// <summary>
/// Simplifies creating an NotImplementedException with a message created via String.Format().
/// <para>Instead of calling 'throw new NotImplementedException(String.Format("", ...))', this class's
/// constructor eliminates the need to call String.Format(), i.e. 'throw new NotImplementedExceptionFmt("", ...)'.</para>
/// </summary>
public class NotImplementedExceptionFmt(String message, params Object[] args) : NotImplementedException(String.Format(message, args))
{
}

public class ItemNotFoundException(String message) : Exception(message)
{
}

public static class ExceptionUtils
{
  /// <summary>
  /// Recursively gather up all data about the given exception <paramref name="ex"/>, including inner exceptions and
  /// whatever's stored in ex's Data property, and return it all as a <see cref="String"/>.
  /// <para>Useful for logging exception data.</para>
  /// </summary>
  /// <param name="ex">A reference to an <see cref="Exception"/>, or one of its descendents.</param>
  /// <returns>A <see cref="String"/> containing the exception message, all inner exception messages, and any data contained in the exception's Data property.</returns>
  public static String GetAllExceptionMessages(this Exception ex)
  {
    var result = new List<String>();

    void rec(Exception currentException)
    {
      if (currentException is null)
        return;

      result.Add(currentException.Message);

      foreach (DictionaryEntry de in currentException.Data)
        result.Add($"  {de.Key}: {de.Value}");

      /* StackTrace might be null when running this code in NUnit. */
      if (currentException.StackTrace != null)
        result.Add(String.Format(Properties.Resources.Exceptions_StackTrace, currentException.StackTrace.ToString()));

      rec(currentException.InnerException);
    }

    rec(ex);
    return result.Join("\n");
  }
}

