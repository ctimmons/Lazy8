/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Threading;

namespace Lazy8.Core;

public class StringMessageEventArgs(String message) : EventArgs()
{
  public String Message { get; set; } = message;
}

public static class EventArgsExtensions
{
  /* Thread-safe way to raise an event from "CLR via C#, 4th Edition"
      by Jeffrey Richter, pp. 254-255. */
  public static void Raise<TEventArgs>(this TEventArgs eventArgs, Object sender, ref EventHandler<TEventArgs> handler)
    where TEventArgs : EventArgs
  {
    EventHandler<TEventArgs> temp = Volatile.Read(ref handler);

    temp.Invoke(sender, eventArgs);
  }
}

