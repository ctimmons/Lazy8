/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace Lazy8.Core.Tests.FileIO.Immutable;

/* This [SetUpFixture] and all of the following [TestFixtures] are scoped to the above namespace.

   See:
     https://stackoverflow.com/a/58963378/116198
     https://docs.nunit.org/articles/nunit/writing-tests/attributes/setupfixture.html

   That means this setup fixture can encapsulate the entire test environment for all of the tests
   in this namespace. */

[SetUpFixture]
public partial class TestEnvironment
{
  public static readonly String TestFilesPath = FileUtils.GetTemporarySubfolder();

  public static readonly String Level_1_1 = Path.Combine(TestFilesPath, @"level_1.1");
  public static readonly String Level_1_2 = Path.Combine(TestFilesPath, @"level_1.2");
  public static readonly String Level_2_2 = Path.Combine(TestFilesPath, @"level_1.1\level_2.2");
  public static readonly String Level_3_1 = Path.Combine(TestFilesPath, @"level_1.1\level_2.1\level_3.1");
  public static readonly String Level_3_2 = Path.Combine(TestFilesPath, @"level_1.1\level_2.1\level_3.2");

  public static readonly Int32 TotalNumberOfLevel_0Subdirectories = 0;
  public static readonly Int32 TotalNumberOfLevel_1Subdirectories = 6;
  public static readonly Int32 TotalNumberOfLevel_1TopLevelDirectories = 2;
  public static readonly Int32 TotalNumberOfLevel_2Subdirectories = 4;
  public static readonly Int32 TotalNumberOfLevel_3Subdirectories = 2;

  public static readonly Int32 TotalNumberOfFoxAndDogFiles = 2;
  public static readonly Int32 TotalNumberOfHelloWorldFiles = 3;

  [GeneratedRegex("_0", RegexOptions.Singleline)]
  public static partial Regex Level_0_NameRegex();

  [GeneratedRegex("_1", RegexOptions.Singleline)]
  public static partial Regex Level_1_NameRegex();

  [GeneratedRegex("_2", RegexOptions.Singleline)]
  public static partial Regex Level_2_NameRegex();

  [GeneratedRegex("_3", RegexOptions.Singleline)]
  public static partial Regex Level_3_NameRegex();

  [GeneratedRegex("_dog", RegexOptions.Singleline)]
  public static partial Regex Dog_NameRegex();

  [GeneratedRegex("_world", RegexOptions.Singleline)]
  public static partial Regex World_NameRegex();

  public static readonly List<Regex> Levels_1_and_3_NameRegexes = [Level_1_NameRegex(), Level_3_NameRegex()];
  public static readonly Int32 TotalNumberOfLevel_1AndLevel_3Subdirectories = TotalNumberOfLevel_1Subdirectories + TotalNumberOfLevel_3Subdirectories;

  [OneTimeSetUp]
  public void Init()
  {
    /* Set up an environment of folders and files that most of the unit tests use when they run.
       Note that this will result in 6 subdirectories under _testFilesPath. */

    Directory.CreateDirectory(Level_1_2);
    Directory.CreateDirectory(Level_2_2);
    Directory.CreateDirectory(Level_3_1);
    Directory.CreateDirectory(Level_3_2);

    /* None of the immutable file I/O tests examine the contents of files,
       so just write an empty string. */

    File.WriteAllText(Path.Combine(Level_1_1, "fox_and_dog.txt"), "");
    File.WriteAllText(Path.Combine(Level_1_2, "hello_world.txt"), "");
    File.WriteAllText(Path.Combine(Level_2_2, "hello_world.txt"), "");
    File.WriteAllText(Path.Combine(Level_3_1, "fox_and_dog.txt"), "");
    File.WriteAllText(Path.Combine(Level_3_2, "hello_world.txt"), "");
  }

  [OneTimeTearDown]
  public void Cleanup()
  {
    if (Directory.Exists(TestFilesPath))
      Directory.Delete(TestFilesPath, recursive: true);
  }
}

public partial class Immutable_FileIO_Tests
{
  /* There are numerous tests, all of which test overloaded methods that fall naturally into groups.
     Therefore I've grouped the tests into classes, where both the classes and test methods have
     a specific naming convention.

     Class names consist of the group of methods being tested, followed by those methods' return type.
     For example, the group of methods named GetDirectories that all return a string array
     are all tested in a class named GetDirectories_ReturnsStringArray.

     The test method names have a similar structure.  However, since the enclosing class name already
     includes the method's name and return type, they are elided from the test method name.
     Therfore the test method name consists only of the methods' parameter types.
     For example, the method 'String[] GetDirectories(String directory, Regex regex)' is tested
     by the method 'public void Directory_Regex_Test()' in the GetDirectories_ReturnsStringArray class.
  
     Also note that the tested methods consist largely of overloads, so only one method in each group of
     overloads needs to be tested to ensure comprehensive test coverage.  For example, the following three
     methods contain two overloads, and one that does the actual work:

       // overload #1
       public static String[] GetDirectories(String directory, Regex regex)

       // overload #2
       public static String[] GetDirectories(String directory, Regex regex, SearchOption searchOption)

       // actual work
       public static String[] GetDirectories(String directory, Regex regex, EnumerationOptions enumerationOptions)

     In most cases, only overload #2 needs to be tested by the GetDirectories_ReturnsStringArray::Directory_Regex_SearchOption_Test()
     method.  Unless otherwise stated, that's the general pattern the tests in this file take. */

  public class GetDirectories_ReturnsStringArray
  {
    [Test]
    public void Directory_Regex_SearchOption_Test()
    {
      /* Test all of the ways the directory parameter can fail. */
      Assert.That(() => FileUtils.GetDirectories((String) null!, TestEnvironment.Level_1_NameRegex()), Throws.TypeOf<ArgumentNullException>());
      Assert.That(() => FileUtils.GetDirectories("", TestEnvironment.Level_1_NameRegex()), Throws.TypeOf<ArgumentException>());
      Assert.That(() => FileUtils.GetDirectories("   ", TestEnvironment.Level_1_NameRegex()), Throws.TypeOf<ArgumentException>());

      /* Also test how the regex parameter can fail. */
      Assert.That(() => FileUtils.GetDirectories(TestEnvironment.TestFilesPath, (Regex) null!), Throws.TypeOf<ArgumentNullException>());

      var actual = FileUtils.GetDirectories(TestEnvironment.TestFilesPath, TestEnvironment.Level_2_NameRegex(), SearchOption.TopDirectoryOnly).Length;
      var expected = 0;
      Assert.That(actual == expected, Is.True);

      actual = FileUtils.GetDirectories(TestEnvironment.TestFilesPath, TestEnvironment.Level_0_NameRegex(), SearchOption.AllDirectories).Length;
      expected = TestEnvironment.TotalNumberOfLevel_0Subdirectories;
      Assert.That(actual == expected, Is.True);

      actual = FileUtils.GetDirectories(TestEnvironment.TestFilesPath, TestEnvironment.Level_2_NameRegex(), SearchOption.AllDirectories).Length;
      expected = TestEnvironment.TotalNumberOfLevel_2Subdirectories;
      Assert.That(actual == expected, Is.True);
    }

    [Test]
    public void Directory_IEnumerableRegexes_SearchOption_Test()
    {
      /* Test all of the ways the directory parameter can fail. */
      Assert.That(() => FileUtils.GetDirectories((String) null!, TestEnvironment.Level_1_NameRegex()), Throws.TypeOf<ArgumentNullException>());
      Assert.That(() => FileUtils.GetDirectories("", TestEnvironment.Level_1_NameRegex()), Throws.TypeOf<ArgumentException>());
      Assert.That(() => FileUtils.GetDirectories("   ", TestEnvironment.Level_1_NameRegex()), Throws.TypeOf<ArgumentException>());

      /* Also test how the regex parameter can fail. */
      Assert.That(() => FileUtils.GetDirectories(TestEnvironment.TestFilesPath, (Regex) null!), Throws.TypeOf<ArgumentNullException>());

      var actual = FileUtils.GetDirectories(TestEnvironment.TestFilesPath, TestEnvironment.Levels_1_and_3_NameRegexes, SearchOption.TopDirectoryOnly).Length;
      var expected = TestEnvironment.TotalNumberOfLevel_1TopLevelDirectories;
      Assert.That(actual == expected, Is.True);

      actual = FileUtils.GetDirectories(TestEnvironment.TestFilesPath, TestEnvironment.Levels_1_and_3_NameRegexes, SearchOption.AllDirectories).Length;
      expected = TestEnvironment.TotalNumberOfLevel_1AndLevel_3Subdirectories;
      Assert.That(actual == expected, Is.True);
    }
  }
}
