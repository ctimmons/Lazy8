/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Lazy8.Core
{
  /* A collection of miscellaneous code that doesn't really fit anywhere else. */

  public enum RunProcessType { IgnoreResult, ReturnResult }

  public static class GeneralUtils
  {
    /// <summary>
    /// Executes <paramref name="command"/> (plus any <paramref name="arguments"/>) in a system <see cref="Process"/>.  The process's output can be ignored or returned
    /// depending on the value of <paramref name="runProcessType"/>.
    /// <para>By default, the process will wait for an infinite amount of time to complete the operation.
    /// This behavior can be changed by passing a postive value in <paramref name="timeoutInMilliseconds"/>.</para>
    /// </summary>
    /// <param name="command">A <see cref="String"/> containing the command to execute.</param>
    /// <param name="arguments">An optional list of arguments to pass to <paramref name="command"/>.</param>
    /// <param name="runProcessType">A <see cref="RunProcessType"/> enumeration value indicating whether to ignore or return the result created by the process.</param>
    /// <returns>If the result is to be ignored, null is returned.  Otherwise, a <see cref="String"/> containing the result created by the process is returned.</returns>
    public static (Int32 ExitCode, String Output) RunProcess(String command, String arguments, RunProcessType runProcessType, Int32 timeoutInMilliseconds = Timeout.Infinite)
    {
      // todo: expand to read stderr, include in output tuple (or struct/class). add param to allow combining stdout and stderr into Output member.
      var psi =
        new ProcessStartInfo()
        {
          FileName = command,
          Arguments = arguments,
          RedirectStandardOutput = (runProcessType == RunProcessType.ReturnResult),
          UseShellExecute = false
        };

      using (var process = Process.Start(psi))
      {
        switch (runProcessType)
        {
          case RunProcessType.IgnoreResult:
            process.WaitForExit(timeoutInMilliseconds);
            process.WaitForExit();
            return (process.ExitCode, null);
          case RunProcessType.ReturnResult:
            /* Avoid deadlocks by reading the entire standard output stream and
               then waiting for the process to exit.  See the "Remarks" section
               in the MSDN documentation:
                 https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardoutput
            */
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(timeoutInMilliseconds);
            process.WaitForExit();
            return (process.ExitCode, output);
          default:
            throw new ArgumentException(String.Format(Properties.Resources.Utils_UnknownRunProcessType, runProcessType));
        }
      }
    }

    /* See the Remarks section for Assembly.GetCallingAssembly() as to why
       MethodImplAttribute is needed.
         (https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.getcallingassembly?view=netframework-4.8#remarks)
       
       Tip: To get the correct name of an embedded resource, use ILSpy (http://www.ilspy.net/).
            Look in the assembly's "Resources" folder. */

    /// <summary>
    /// The embedded text referred to by <paramref name="resourceName"/> is returned.
    /// </summary>
    /// <param name="resourceName">A <see cref="String"/> naming the embedded text resource to be returned.</param>
    /// <returns>The embedded text resource.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static String GetEmbeddedTextResource(this String resourceName)
    {
      using (var sr = new StreamReader(Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName)))
        return sr.ReadToEnd();
    }

    /// <summary>
    /// Returns a <see cref="Boolean"/> indicating if the <paramref name="assembly"/> is JIT optimized or not.
    /// </summary>
    /// <param name="assembly">An <see cref="Assembly"/> reference.</param>
    /// <returns>A <see cref="Boolean"/> indicating if the <paramref name="assembly"/> is JIT optimized or not.</returns>
    public static Boolean IsJITOptimized(this Assembly assembly)
    {
      foreach (var attribute in assembly.GetCustomAttributes(typeof(DebuggableAttribute), false))
        if (attribute is DebuggableAttribute)
          return !(attribute as DebuggableAttribute).IsJITOptimizerDisabled;

      return true;
    }

    /// <summary>
    /// Naive function that recursively enumerates the objects params array,
    /// building a new XOR-ed hashcode of all of the object instances it contains.
    /// </summary>
    /// <param name="objects">A array of zero or more <see cref="Object"/>s.</param>
    /// <returns>An <see cref="Int32"/> representing the XOR-ed hashcode of all object instances contained in the <paramref name="objects"/> array.</returns>
    public static Int32 GetHashCode(params Object[] objects)
    {
      static Int32 rec(Int32 hashcode, Object obj)
      {
        if (obj is IEnumerable)
        {
          /* Recursive case. */
          foreach (var o in (obj as IEnumerable))
            hashcode = rec(hashcode, o);

          return hashcode;
        }
        else
        {
          /* Base case. */
          return hashcode ^ obj.GetHashCode();
        }
      }

      return rec(0, objects);
    }

    /// <summary>
    /// Return the method name for the given <paramref name="stackFrameLevel"/> (greater than zero).
    /// </summary>
    /// <param name="stackFrameLevel">The stack frame to retrieve the method name from.  Must be greater than zero.</param>
    /// <returns>The name of the method that created the given stack frame.</returns>
    public static String GetMethodName(Int32 stackFrameLevel = 1)
    {
      var sf = new StackFrame(stackFrameLevel);
      var mb = sf.GetMethod();
      return $"{mb.DeclaringType.FullName}.{mb.Name}";
    }

    /// <summary>
    /// Get a String containing stack information from zero or more <paramref name="levels"/> up the call stack.
    /// <para>The default value of 2 for <paramref name="levels"/> will return
    /// the caller's stack frame data.</para>
    /// </summary>
    /// <param name="levels">
    /// A <see cref="Int32"/> which indicates how many levels up the stack 
    /// the information should be retrieved from.  This value must be zero or greater.
    /// </param>
    /// <returns>
    /// A String in this format:
    /// <para>
    /// file name::namespace.[one or more type names].method name
    /// </para>
    /// </returns>
    public static String GetStackInfo(Int32 levels = 2)
    {
      var sf = new StackFrame(levels, true /* Get the file name, line number, and column number of the stack frame. */);
      var mb = sf.GetMethod();

      return $"{Path.GetFileName(sf.GetFileName())}::{mb.DeclaringType.FullName}.{mb.Name} - Line {sf.GetFileLineNumber()}";
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public static T GetAssemblyAttribute<T>(this Assembly assembly)
      where T : Attribute
    {
      var attributes = assembly.GetCustomAttributes(typeof(T), true);

      return
        ((attributes == null) || (attributes.Length == 0))
        ? null
        : (T) attributes[0];
    }
  }
}
