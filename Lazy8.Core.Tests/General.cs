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

    Assert.That(() => new RunProcessInfo() { Arguments = null }, Throws.ArgumentException);
    Assert.That(() => new RunProcessInfo() { TimeoutInSeconds = -2 }, Throws.ArgumentException);
    Assert.That(() => new RunProcessInfo() { StdOutPredicate = null }, Throws.ArgumentException);
    Assert.That(() => new RunProcessInfo() { StdErrPredicate = null }, Throws.ArgumentException);
  }

  private readonly Int32 _expectedExitCode = 99;
  private readonly String _expectedStdOut = "standard output";
  private readonly String _expectedStdErr = "standard error";

  [GeneratedRegex(@"CS\d{4}:")]
  private static partial Regex CsWarningRegex();

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

      var runProcessOutput = GeneralUtils.RunProcess(runProcessInfo);

      Assert.That(runProcessOutput.ExitCode, Is.EqualTo(this._expectedExitCode));
      Assert.That(runProcessOutput.StdOutput, Is.EqualTo(this._expectedStdOut));
      Assert.That(runProcessOutput.StdError, Is.EqualTo(this._expectedStdErr));
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
    <TargetFramework>net8.0</TargetFramework>
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