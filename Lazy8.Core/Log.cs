/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.IO;

namespace Lazy8.Core
{
  public enum LogEntryType
  {
    Error = 1,
    Warning = 2,
    Information = 4
  }

  /* The Log class provides a very simple logging facility.  It's only intended
     to be used when larger and more complex logging libraries are not appropriate.

     NOTE: Any TextWriter descendent passed to the Log constructor will
           NOT be closed or disposed of when the instance of Log is disposed.
  
     Examples:

       // Writing log data to a file.
       using (var sw = new StreamWriter("my_log_file.txt"))
       {
         var log = new Log(sw);
         log.WriteLine(LogEntryType.Info, "Hello, world!");
       }

       // Writing log data to the console.
       var log = new Log(Console.Out);
       log.WriteLine(LogEntryType.Info, "Hello, world!");
  */

  public class Log
  {
    private readonly TextWriter _writer;

    private Log()
      : base()
    {
    }

    public Log(TextWriter writer)
      : this()
    {
      writer.Name(nameof(writer)).NotNull();

      this._writer = writer;
    }

    public void WriteLine(LogEntryType logEntryType, String message)
    {
      /* Timestamps are represented in the Round Trip Format Specifier
         (http://msdn.microsoft.com/en-us/library/az4se3k1.aspx#Roundtrip). */
      var timestamp = DateTime.Now.ToUniversalTime().ToString("o");

      var type = logEntryType switch
      {
        LogEntryType.Information => "INF",
        LogEntryType.Warning => "WRN",
        LogEntryType.Error => "ERR",
        _ => "UNK",
      };

      this._writer!.WriteLine($"{timestamp} - {type} - {message}");
      this._writer.Flush();
    }

    public void WriteLine(LogEntryType logEntryType, String message, params Object[] args)
    {
      this.WriteLine(logEntryType, String.Format(message, args));
    }
  }
}
