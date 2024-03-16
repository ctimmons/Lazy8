/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Lazy8.Core;

/// <summary>
/// A class passed to the GeneralUtils.RunProcess() method whose properties direct that method's behavior.
/// </summary>
public class RunProcessInfo
{
  /// <summary>
  /// The command that is to be executed.  Do not include arguments in this command string - use the Arguments property for that.
  /// <para>There is no default for this property.  If this property is null, contains only
  /// an empty string, or a string consisting entirely of whitespace, then an exception will be thrown
  /// when the RunProcess() method is executed.</para>
  /// </summary>
  public String? Command { get; set; }

  private String _arguments = "";
  /// <summary>
  /// Contains the arguments that should be passed to Command when it is executed.
  /// <para>Defaults to an empty string.  Upon assignment, the beginning and end of this property is trimmed.
  /// Attempting to assign null will throw an ArgumentException.</para>
  /// </summary>
  public String Arguments
  {
    get
    {
      return this._arguments;
    }
    set
    {
      if (value is null)
        throw new ArgumentException(String.Format(Properties.Resources.Utils_CannotAssignNullToProperty, nameof(this.Arguments)));
      else
        this._arguments = value.Trim();
    }
  }

  private Int32 _timeoutInSeconds = Timeout.Infinite;
  /// <summary>
  /// The maximum allowed time (in seconds) the Command is allowed to run before timing out.
  /// <para>The default value is Timeout.Infinite (-1).  A value of zero specifies an immediate return
  /// (in general, that's usually not the desired behavior).
  /// Attempting to assign a value of less than -1 will throw an ArgumentException.</para>
  /// </summary>
  public Int32 TimeoutInSeconds
  {
    get
    {
      return this._timeoutInSeconds;
    }
    set
    {
      if (value < Timeout.Infinite)
        throw new ArgumentException(Properties.Resources.Utils_CannotAssignValueLessThanNegativeOne);
      else
        this._timeoutInSeconds = value;
    }
  }

  /// <summary>
  /// Readonly property indicating the process's timeout in milliseconds.
  /// <para>This property is calculated from TimeoutInSeconds's value, and is only meaningful for TimeoutInSeconds
  /// values of zero or greater.  If TimeoutInSeconds is set to Timeout.Infinite (-1), this property will also be set to -1.</para>
  /// </summary>
  public Int32 TimeoutInMilliseconds => Math.Max(this.TimeoutInSeconds * 1000, Timeout.Infinite);

  /// <summary>
  /// A default Predicate that always returns true.
  /// </summary>
  public static readonly Predicate<String> DefaultStdOutPredicate = _ => true;

  private Predicate<String> _stdOutPredicate = DefaultStdOutPredicate;
  /// <summary>
  /// A Predicate that takes a String and returns a Boolean value.
  /// <para>The data Command sends to stdout is line oriented.  This predicate is applied to each line of that ouput.
  /// If the predicate returns true, the line is included in the result returned by the RunProcess() method.  Likewise, if the
  /// predicate returns false, the line of text is not included in RunProcess()'s return value.</para>
  /// <para>This property defaults to the simple lambda '(String line) => true'.  The property may be set to a different
  /// lambda, but may not be set to null.  Attempting to assign null will throw an ArgumentException.</para>
  /// </summary>
  public Predicate<String> StdOutPredicate
  {
    get
    {
      return this._stdOutPredicate;
    }
    set
    {
      if (value is null)
        throw new ArgumentException(String.Format(Properties.Resources.Utils_CannotAssignNullToProperty, nameof(this.StdOutPredicate)));
      else
        this._stdOutPredicate = value;
    }
  }

  /// <summary>
  /// A default Predicate that always returns true.
  /// </summary>
  public static readonly Predicate<String> DefaultStdErrPredicate = _ => true;

  private Predicate<String> _stdErrPredicate = DefaultStdErrPredicate;
  /// <summary>
  /// A Predicate that takes a String and returns a Boolean value.
  /// <para>The data Command sends to stderr is line oriented.  This predicate is applied to each line of that ouput.
  /// If the predicate returns true, the line is included in the result returned by the RunProcess() method.  Likewise, if the
  /// predicate returns false, the line of text is not included in RunProcess()'s return value.</para>
  /// <para>This property defaults to the simple lambda '(String line) => true'.  The property may be set to a different
  /// lambda, but may not be set to null.  Attempting to assign null will throw an ArgumentException.</para>
  /// </summary>
  public Predicate<String> StdErrPredicate
  {
    get
    {
      return this._stdErrPredicate;
    }
    set
    {
      if (value is null)
        throw new ArgumentException(String.Format(Properties.Resources.Utils_CannotAssignNullToProperty, nameof(this.StdErrPredicate)));
      else
        this._stdErrPredicate = value;
    }
  }
}

/// <summary>
/// A record struct returned by the RunProcess() method upon successful completion.
/// </summary>
/// <param name="ExitCode">An Int32 containing the exit code of the executed process.</param>
/// <param name="StdOutput">A String containing the filtered output of the executed process's stdout stream.
/// (See RunProcessInfo.StdOutPredicate for a description of the filter lambda that can be applied.  The default
/// captures all of the process's stdout data.)</param>
/// <param name="StdError">A String containing the filtered output of the executed process's stderr stream.
/// (See RunProcessInfo.StdErrPredicate for a description of the filter lambda that can be applied.  The default
/// captures all of the process's stdout data.)</param>
public readonly record struct RunProcessOutput(Int32 ExitCode, String StdOutput, String StdError);

/// <summary>
/// A collection of methods that don't seem to fit anywhere else in this library.
/// </summary>
public static class GeneralUtils
{
  /// <summary>
  /// Executes <paramref name="command"/> in a system <see cref="Process"/>.
  /// <para>By default, the process will wait for an infinite amount of time to complete the operation.</para>
  /// </summary>
  /// <param name="command">A <see cref="String"/> containing the command to execute.</param>
  /// <returns>A RunProcessOutput struct containing the process's exit code, standard output, and standard error output.</returns>
  public static RunProcessOutput RunProcess(String command) =>
    RunProcess(new RunProcessInfo() { Command = command });

  /// <summary>
  /// Executes <paramref name="command"/> in a system <see cref="Process"/>.
  /// <para>By default, the process will wait for an infinite amount of time to complete the operation.
  /// This behavior can be changed by passing a postive value in <paramref name="timeoutInSeconds"/>.</para>
  /// </summary>
  /// <param name="command">A <see cref="String"/> containing the command to execute.</param>
  /// <param name="timeoutInSeconds">An Int32 indicating the timeout in seconds.  Values less than Timeout.Infinite (-1) raise an exception.</param>
  /// <returns>A RunProcessOutput struct containing the process's exit code, standard output, and standard error output.</returns>
  public static RunProcessOutput RunProcess(String command, Int32 timeoutInSeconds = Timeout.Infinite) =>
    RunProcess(new RunProcessInfo() { Command = command, TimeoutInSeconds = timeoutInSeconds });

  /// <summary>
  /// Executes <paramref name="command"/> (plus any <paramref name="arguments"/>) in a system <see cref="Process"/>.
  /// <para>By default, the process will wait for an infinite amount of time to complete the operation.
  /// This behavior can be changed by passing a postive value in <paramref name="timeoutInSeconds"/>.</para>
  /// </summary>
  /// <param name="command">A <see cref="String"/> containing the command to execute.</param>
  /// <param name="arguments">An optional String of arguments to pass to <paramref name="command"/> (can be null or an empty string).</param>
  /// <param name="timeoutInSeconds">An Int32 indicating the timeout in seconds.  Values less than Timeout.Infinite (-1) raise an exception.</param>
  /// <returns>A RunProcessOutput struct containing the process's exit code, standard output, and standard error output.</returns>
  public static RunProcessOutput RunProcess(String command, String arguments, Int32 timeoutInSeconds = Timeout.Infinite) =>
    RunProcess(new RunProcessInfo() { Command = command, Arguments = arguments, TimeoutInSeconds = timeoutInSeconds });

  /// <summary>
  /// Executes <paramref name="runProcessInfo.Command"/> (plus any <paramref name="runProcessInfo.Arguments"/>) in a system <see cref="Process"/>.
  /// <para>By default, the process will wait for an infinite amount of time to complete the operation.
  /// This behavior can be changed by passing a postive value in <paramref name="runProcessInfo.TimeoutInSeconds"/>.</para>
  /// <para>runProcessInfo also provides two Boolean predicates that allow for the filtering of the stdout and stderr
  /// results generated by the program.</para>
  /// </summary>
  /// <param name="runProcessInfo">An instance of RunProcessInfo.  The instance's Command property must be provided, and a TimeoutInSeconds
  /// value greater than or equal to Timeout.Infinite (-1).  The other properties are optional.</param>
  /// <returns>A RunProcessOutput struct containing the process's exit code, standard output, and standard error output.</returns>
  public static RunProcessOutput RunProcess(RunProcessInfo runProcessInfo)
  {
    runProcessInfo.Name(nameof(runProcessInfo)).NotNull();
    runProcessInfo.Command!.Name(nameof(runProcessInfo.Command)).NotNullEmptyOrOnlyWhitespace();

    /* RunProcess() is a synchronous method.  To run an external process in an ASYNCHRONOUS manner is non-trivial.
       Below are links to some code and discussions about how to do that.
    
       I'm not going to bother implementing an asynchronous version of this method until I reeeeeaaaaallly need to.
       There are way too many ways subtle bugs can creep into such an algorithm.  A quick-n-dirty way to run this
       method asynchronously is to wrap it in a Task.Run() call - the drawback being that it won't
       be cancellable after the work is started.
    
       https://gist.github.com/AlexMAS/276eed492bc989e13dcce7c78b9e179d
       https://gist.github.com/georg-jung/3a8703946075d56423e418ea76212745
       https://stackoverflow.com/questions/470256/process-waitforexit-asynchronously */

    StringBuilder stdout = new();
    StringBuilder stderr = new();

    ProcessStartInfo psi =
      new()
      {
        FileName = runProcessInfo.Command,
        Arguments = runProcessInfo.Arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

    using (var process = Process.Start(psi))
    {
      if (process is null)
        throw new Exception(String.Format(Properties.Resources.Utils_ProcessCouldNotBeStarted, $"{runProcessInfo.Command} {runProcessInfo.Arguments}".Trim()));

      /* Use StringBuilder's Append(), not AppendLine(), method to add data to the stdout and stderr accumulators.
         AppendLine() adds an Environment.NewLine to the end of the line, which is not something generated by the
         program being called.  Append() has no such side-effects. */

      process.OutputDataReceived += (_, e) => { if ((e.Data != null) && runProcessInfo.StdOutPredicate(e.Data)) stdout.Append(e.Data); };
      process.BeginOutputReadLine();

      process.ErrorDataReceived += (_, e) => { if ((e.Data != null) && runProcessInfo.StdErrPredicate(e.Data)) stderr.Append(e.Data); };
      process.BeginErrorReadLine();

      if (process.WaitForExit(runProcessInfo.TimeoutInMilliseconds))
      {
        /* This code might look a bit weird - why is WaitForExit() being
           called immediately after a successful call to WaitForExit(timeoutInMilliseconds)?

           It's because the ErrorDataReceived and OutputDataReceived event handlers
           are asynchronous.  They are asychronous because they were redirected
           via the asychronous methods BeginErrorReadLine() and BeginOutputReadLine().

           From the "Remarks" section for WaitForExit():

             "This overload ensures that all processing has been completed, including the handling
             of asynchronous events for redirected standard output. You should use this overload
             after a call to the WaitForExit(Int32) overload when standard output has been
             redirected to asynchronous event handlers."

           https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit
        */

        process.WaitForExit();

        return new RunProcessOutput(process.ExitCode, stdout.ToString(), stderr.ToString());
      }
      else
      {
        throw new Exception(String.Format(Properties.Resources.Utils_ProcessTimedOut, $"{runProcessInfo.Command} {runProcessInfo.Arguments}".Trim(), runProcessInfo.TimeoutInSeconds));
      }
    }
  }

  /* See the Remarks section for Assembly.GetCallingAssembly() as to why
     MethodImplAttribute is needed.
       
       https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.getcallingassembly

     Tip: To get the correct name of an embedded resource, load the assembly into ILSpy (http://www.ilspy.net/).
          Look in the assembly's "Resources" folder. */

  /// <summary>
  /// The embedded text referred to by <paramref name="resourceName"/> is returned.
  /// </summary>
  /// <param name="resourceName">A <see cref="String"/> naming the embedded text resource to be returned.</param>
  /// <returns>The embedded text resource.</returns>
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static String GetEmbeddedTextResource(this String resourceName)
  {
    var resourceStream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName);
    if (resourceStream is null)
      return "";
    else
      using (var sr = new StreamReader(resourceStream))
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
      if (attribute is DebuggableAttribute debuggableAttribute)
        return !debuggableAttribute.IsJITOptimizerDisabled;

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
      if (obj is IEnumerable enumerable)
      {
        /* Recursive case. */
        foreach (var o in enumerable)
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

  private static void ValidateStackFrame(StackFrame sf, Int32 stackFrameLevel)
  {
    /* I know this looks weird - how can a constructor return null?
       Well, somehow it can.  It makes sense in that the requested stackFrameLevel does not exist.
       This can happen in a release build where the stack frame has been optimized away. */
    if (sf is null)
      throw new Exception(String.Format(Properties.Resources.Utils_NoStackFrameExists, stackFrameLevel));

    /* GetMethod() may return null.  This can happen in release builds where the actual
       method call has been optimized away. */
    var mb = sf.GetMethod() ?? throw new Exception(String.Format(Properties.Resources.Utils_NoMethodFoundOnStackFrame, stackFrameLevel));

    /* Like GetMethod(), mbDeclaringType can be optimized away in release builds. */
    if (mb.DeclaringType is null)
      throw new Exception(String.Format(Properties.Resources.Utils_NoDeclaringTypeFoundOnStackFrame, stackFrameLevel));
  }

  /// <summary>
  /// Return the method name for the given <paramref name="stackFrameLevel"/> (greater than zero).
  /// </summary>
  /// <param name="stackFrameLevel">The stack frame to retrieve the method name from.  Must be greater than zero.</param>
  /// <returns>The name of the method that created the given stack frame.</returns>
  public static String GetMethodName(Int32 stackFrameLevel = 1)
  {
    stackFrameLevel.Name(nameof(stackFrameLevel)).GreaterThan(0);

    var sf = new StackFrame(stackFrameLevel);

    ValidateStackFrame(sf, stackFrameLevel);

    var mb = sf.GetMethod();

    return $"{mb!.DeclaringType!.FullName}.{mb.Name}";
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
    levels.Name(nameof(levels)).GreaterThanOrEqualTo(0);

    var sf = new StackFrame(levels, true /* Get the file name, line number, and column number of the stack frame. */);

    ValidateStackFrame(sf, levels);

    var mb = sf.GetMethod();

    return $"{Path.GetFileName(sf.GetFileName())}::{mb!.DeclaringType!.FullName}.{mb.Name} - Line {sf.GetFileLineNumber()}";
  }

  /// <summary>
  /// Get the first instance of (T : Attribute) in the given assembly.
  /// </summary>
  /// <typeparam name="T">A type that descends from System.Attribute.</typeparam>
  /// <param name="assembly">An assembly.</param>
  /// <returns>An instance of T : Attribute.</returns>
  public static T? GetAssemblyAttribute<T>(this Assembly assembly)
    where T : Attribute
  {
    var attributes = assembly.GetCustomAttributes(typeof(T), true);

    return
      ((attributes is null) || (attributes.Length == 0))
      ? null
      : (T) attributes[0];
  }
}

