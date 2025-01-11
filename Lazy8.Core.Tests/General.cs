/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace Lazy8.Core.Tests;

[TestFixture]
public partial class GeneralUtilsTests
{
  [Test]
  public void RunProcessInfoTest()
  {
    Assert.That(() => new RunProcessInfo() { Arguments = "Hello world" }, Throws.Nothing);
    Assert.That(() => new RunProcessInfo() { TimeoutInSeconds = 42 }, Throws.Nothing);
    Assert.That(() => new RunProcessInfo() { StdOutPredicate = line => line.Length > 42 }, Throws.Nothing);
    Assert.That(() => new RunProcessInfo() { StdErrPredicate = line => line.Length < 69 }, Throws.Nothing);

    Assert.That(() => new RunProcessInfo() { Arguments = null! }, Throws.ArgumentException);
    Assert.That(() => new RunProcessInfo() { TimeoutInSeconds = -2 }, Throws.ArgumentException);
    Assert.That(() => new RunProcessInfo() { StdOutPredicate = null! }, Throws.ArgumentException);
    Assert.That(() => new RunProcessInfo() { StdErrPredicate = null! }, Throws.ArgumentException);
  }

  private readonly Int32 _expectedExitCode = 99;
  private readonly String _expectedStdOut = "standard output";
  private readonly String _expectedStdErr = "standard error";

  [GeneratedRegex(@"CS\d{4}:")]
  private static partial Regex CsWarningRegex();

  [GeneratedRegex(@"\u001b](?:.*?)\u001b\\")]
  private static partial Regex OperatingSystemCommandsRegex();

  [Test]
  public void RunProcessTest()
  {
    /* Try to execute a program that does not exist.
       (If "alskdjasdzxcnvzljosfopcpfjouwerljasfjijafsdcxxce" actually exists as an executable on your system,
       you should start buying lottery tickets.) */

    Assert.Throws<Win32Exception>(() => GeneralUtils.RunProcess("alskdjasdzxcnvzljosfopcpfjouwerljasfjijafsdcxxce"));

    /* Next create a small app in a temporary folder, compile, and run it. */

    var tempFolder = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), null);
    Directory.CreateDirectory(tempFolder);
    var previousCurrentDirectory = Directory.GetCurrentDirectory();
    Directory.SetCurrentDirectory(tempFolder);

    this.CreateTestAppSourceCode(tempFolder);
    try
    {
      RunProcessInfo runProcessInfo =
        new()
        {
          Command = "dotnet",
          Arguments = "run --nologo",
          StdOutPredicate = line => !CsWarningRegex().IsMatch(line)
        };

      /* The .Net SDK has an INCREDIBLY ANNOYING behavior.

         After installing a new SDK, .Net ignores the '--nologo' flag for the FIRST run of any app on the SDK [0].
         It respects the '--nologo' flag on subsequent runs for that app, and any others.

         The first run will unconditionally write something like 'Welcome to .NET 9.0!' to stdout.
         This screws up the assertion for this unit test.

         The solution is this ugly 'for' loop.  If the first run has that stupid welcome message,
         just try again hoping that it won't be present in the second run.
      
         (I'm aware of the DOTNET_NOLOGO environment variable [1].  It *should* enforce the same behavior as
         the '--nologo' flag, but doesn't.  DOTNET_NOLOGO suppresses both the welcome message and logo,
         whereas '--nologo' just supresses the logo message.  It's not worth the aggravation to detect if
         this environment variable is already set, possibly change its setting, and then change it back
         to its original value just for this unit test.  The ugly 'for' loop is sufficient for now.)

           [0] https://github.com/dotnet/sdk/issues/3828
           [1] https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_nologo
      */

      var wereTestsExecuted = false;
      for (var i = 1; i <= 2; i++)
      {
        var runProcessOutput = GeneralUtils.RunProcess(runProcessInfo);

        /* For some unknown reason, StdOutput will sometimes contain Operating
           System Command (OSC) escape codes [0].  I don't know if it's a
           .Net thing or a Windows thing.  Remove them to get the test to pass.

           [0] https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences */

        var sanitizedStdOutput = OperatingSystemCommandsRegex().Replace(runProcessOutput.StdOutput, "");

        if (sanitizedStdOutput.ContainsCI("Welcome to .NET"))
          /* If we received that dumbass 'welcome' message, try again. */
          continue;

        Assert.That(sanitizedStdOutput, Is.EqualTo(this._expectedStdOut));
        Assert.That(runProcessOutput.StdError, Is.EqualTo(this._expectedStdErr));
        Assert.That(runProcessOutput.ExitCode, Is.EqualTo(this._expectedExitCode));

        wereTestsExecuted = true;
      }

      Assert.That(wereTestsExecuted, Is.True);
    }
    finally
    {
      if (Directory.Exists(tempFolder))
      {
        /* Reset the current directory before trying to delete tempFolder.
           If this isn't done, Windows won't allow tempFolder to be deleted because it's still
           the current directory.
        
           This also prevents failures in subsequent unit tests that expect
           the current directory to point to a certain location. (see UU.cs). */

        Directory.SetCurrentDirectory(previousCurrentDirectory);
        Directory.Delete(tempFolder, recursive: true);
      }
    }
  }

  private void CreateTestAppSourceCode(String folder)
  {
    var csProjectFilename = Path.Combine(folder, "program.csproj");
    var csProject = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <IsPublishable>False</IsPublishable>
    <ProduceReferenceAssembly>False</ProduceReferenceAssembly>
    <StartupObject>ConsoleOutput.Program</StartupObject>
  </PropertyGroup>

  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|AnyCPU'"">
    <CheckForOverflowUnderflow>True</CheckForOverflowUnderflow>
  </PropertyGroup>

  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Release|AnyCPU'"">
    <CheckForOverflowUnderflow>True</CheckForOverflowUnderflow>
  </PropertyGroup>

</Project>
".Trim();
    File.WriteAllText(csProjectFilename, csProject);

    var programFilename = Path.Combine(folder, "program.cs");
    var program = @$"
using System;

namespace ConsoleOutput;

public class Program
{{
  public static void Main()
  {{
    /* This will throw a warning about an unused variable to stdout when 'dotnet run'
       is called to compile and run this program.

       The unit test above has to filter the warning out so its assertions pass. */

    var quux = DateTime.Now;

    Console.Out.Write({this._expectedStdOut.DoubleQuote()});
    Console.Error.Write({this._expectedStdErr.DoubleQuote()});
    Environment.Exit({this._expectedExitCode});
  }}
}}
";
    File.WriteAllText(programFilename, program);
  }
}