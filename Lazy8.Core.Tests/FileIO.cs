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

namespace Lazy8.Core.Tests;

[TestFixture]
public class FileUtilsTests
{
  private static readonly String _testFilesPath = FileUtils.GetTemporarySubfolder();
  private static readonly String _testStringsFile = _testFilesPath + "test_strings.txt";
  private static readonly String _testStringFoxAndDog = "The quick brown fox jumped over the lazy dog.";
  private static readonly String _testStringHelloWorld = "Hello, world!";
  private static readonly String _testStrings = (_testStringFoxAndDog + "\n").Repeat(10).Trim();

  private static readonly String _level_1_1 = Path.Combine(_testFilesPath, @"level_1.1");
  private static readonly String _level_1_2 = Path.Combine(_testFilesPath, @"level_1.2");
  private static readonly String _level_2_2 = Path.Combine(_testFilesPath, @"level_1.1\level_2.2");
  private static readonly String _level_3_1 = Path.Combine(_testFilesPath, @"level_1.1\level_2.1\level_3.1");
  private static readonly String _level_3_2 = Path.Combine(_testFilesPath, @"level_1.1\level_2.1\level_3.2");

  public FileUtilsTests()
    : base()
  {
  }

  private String GetTestFilename()
  {
    return Path.Combine(_testFilesPath, Path.GetRandomFileName());
  }

  [SetUp]
  public void Init()
  {
    /* Setup an environment of folders and files that most of the unit tests use when they run. */

    Directory.CreateDirectory(_level_1_2);
    Directory.CreateDirectory(_level_2_2);
    Directory.CreateDirectory(_level_3_1);
    Directory.CreateDirectory(_level_3_2);

    File.WriteAllText(Path.Combine(_level_1_1, "fox_and_dog.txt"), _testStringFoxAndDog);
    File.WriteAllText(Path.Combine(_level_1_2, "hello_world.txt"), _testStringHelloWorld);
    File.WriteAllText(Path.Combine(_level_2_2, "hello_world.txt"), _testStringHelloWorld);
    File.WriteAllText(Path.Combine(_level_3_1, "fox_and_dog.txt"), _testStringFoxAndDog);
    File.WriteAllText(Path.Combine(_level_3_2, "hello_world.txt"), _testStringHelloWorld);

    File.WriteAllText(_testStringsFile, _testStrings);
  }

  [TearDown]
  public void Cleanup()
  {
    if (Directory.Exists(_testFilesPath))
      Directory.Delete(_testFilesPath, true /* Delete all files and subdirectories also. */);
  }

  [Test]
  public void DeleteDirectoryTest()
  {
    var rootDir = Path.Combine(_testFilesPath, "root/folder1/folder2");

    Directory.CreateDirectory(rootDir);

    var readwriteFilename = Path.Combine(rootDir, "dummy1.txt");
    File.WriteAllText(readwriteFilename, _testStringFoxAndDog);

    var readonlyFilename = Path.Combine(rootDir, "dummy2.txt");
    File.WriteAllText(readonlyFilename, _testStringFoxAndDog);
    File.SetAttributes(readonlyFilename, FileAttributes.ReadOnly);

    FileUtils.DeleteDirectory(rootDir);

    Assert.That(Directory.Exists(rootDir), Is.False);
  }

  private void DirectoryWalkerHarness(Action<FileSystemInfo> action, List<String> expected, List<String> actual)
  {
    var exceptions = FileUtils.DirectoryWalker(_testFilesPath, action, FileSystemTypes.All, DirectoryWalkerErrorHandling.ContinueAndAccumulateExceptions);

    if (exceptions.Any())
    {
      var messages = String.Join("\n", exceptions.Select(ex => ex.Message));
      throw new Exception(messages);
    }
    else
    {
      Assert.That(expected.Except(actual).Any(), Is.False);
    }
  }

  [Test]
  public void DirectoryWalkerTest_DeleteFilesAndEmptyDirectories()
  {
    static void action(FileSystemInfo fsi)
    {
      if (fsi is DirectoryInfo di)
      {
        di.DeleteIfEmpty();
      }
      else if (fsi is FileInfo fi)
      {
        if (fi.Name == "hello_world.txt")
          fi.Delete();
      }
    }

    var exceptions = FileUtils.DirectoryWalker(_testFilesPath, action, FileSystemTypes.All, DirectoryWalkerErrorHandling.ContinueAndAccumulateExceptions);

    if (exceptions.Any())
    {
      var messages = String.Join("\n", exceptions.Select(ex => ex.Message));
      throw new Exception(messages);
    }
    else
    {
      /* Contains fox_and_dog.txt, so this directory should still exist. */
      Assert.That(Directory.Exists(_level_3_1), Is.True);

      /* All of these directories contained hello_world.txt, so they should have been deleted. */
      Assert.That(Directory.Exists(_level_1_2), Is.False);
      Assert.That(Directory.Exists(_level_2_2), Is.False);
      Assert.That(Directory.Exists(_level_3_2), Is.False);
    }
  }

  [Test]
  public void DirectoryWalkerTest_GetDirectoryNamesBasedOnFilenamesInDirectory()
  {
    var actual = new List<String>();

    void action(FileSystemInfo fsi)
    {
      if (fsi is DirectoryInfo di)
      {
        if (di.EnumerateFiles().Where(fi => fi.Name.Contains("dog")).Any())
          actual.Add(di.FullName);
      }
    }

    var expected =
      new List<String>()
      {
          _level_1_1,
          _level_3_1
      };

    this.DirectoryWalkerHarness(action, expected, actual);
  }

  [Test]
  public void DirectoryWalkerTest_GetDirectoryNamesBasedOnRegex()
  {
    var actual = new List<String>();

    void action(FileSystemInfo fsi)
    {
      if (fsi is DirectoryInfo di)
      {
        if (Regex.Match(di.FullName, "le..l_1", RegexOptions.Singleline).Success)
          actual.Add(di.FullName);
      }
    }

    var expected =
      new List<String>()
      {
          _level_1_1,
          _level_1_2
      };

    this.DirectoryWalkerHarness(action, expected, actual);
  }

  [Test]
  public void DirectoryWalkerTest_GetFilenamesBasedOnFileContents()
  {
    var actual = new List<String>();

    void action(FileSystemInfo fsi)
    {
      if (fsi is FileInfo fi)
      {
        if (File.ReadAllText(fi.FullName).Contains("fox"))
          actual.Add(fi.FullName);
      }
    }

    var expected =
      new List<String>()
      {
          Path.Combine(_level_1_1, "fox_and_dog.txt"),
          Path.Combine(_level_3_1, "fox_and_dog.txt")
      };

    this.DirectoryWalkerHarness(action, expected, actual);
  }

  [Test]
  public void DirectoryWalkerTest_GetFilenamesBasedOnRegex()
  {
    var actual = new List<String>();

    void action(FileSystemInfo fsi)
    {
      if (fsi is FileInfo fi)
      {
        if (Regex.Match(fi.FullName, "he..o", RegexOptions.Singleline).Success)
          actual.Add(fi.FullName);
      }
    }

    var expected =
      new List<String>()
      {
          Path.Combine(_level_3_2, "hello_world.txt"),
          Path.Combine(_level_2_2, "hello_world.txt"),
          Path.Combine(_level_1_2, "hello_world.txt")
      };

    this.DirectoryWalkerHarness(action, expected, actual);
  }

  [Test]
  public void SafelyCreateEmptyFileTest()
  {
    String filename = null;
    Assert.That(() => FileUtils.SafelyCreateEmptyFile(filename), Throws.TypeOf<ArgumentNullException>());

    filename = "";
    Assert.That(() => FileUtils.SafelyCreateEmptyFile(filename), Throws.TypeOf<ArgumentException>());

    filename = " ";
    Assert.That(() => FileUtils.SafelyCreateEmptyFile(filename), Throws.TypeOf<ArgumentException>());

    filename = Path.Combine(_testFilesPath, Path.GetRandomFileName());
    Assert.That(File.Exists(filename), Is.False);
    FileUtils.SafelyCreateEmptyFile(filename);
    Assert.That(File.Exists(filename), Is.True);
  }

  [Test]
  public void CreateEmptyFileTest()
  {
    String filename = null;
    Assert.That(() => FileUtils.CreateEmptyFile(filename, OverwriteFile.No), Throws.TypeOf<ArgumentNullException>());

    filename = "";
    Assert.That(() => FileUtils.CreateEmptyFile(filename, OverwriteFile.No), Throws.TypeOf<ArgumentException>());

    filename = " ";
    Assert.That(() => FileUtils.CreateEmptyFile(filename, OverwriteFile.No), Throws.TypeOf<ArgumentException>());

    filename = Path.Combine(_testFilesPath, Path.GetRandomFileName());
    Assert.That(File.Exists(filename), Is.False);
    FileUtils.CreateEmptyFile(filename, OverwriteFile.No);
    Assert.That(File.Exists(filename), Is.True);

    File.WriteAllText(filename, _testStringFoxAndDog);
    Assert.That((new FileInfo(filename)).Length > 0, Is.True);
    FileUtils.CreateEmptyFile(filename, OverwriteFile.No);
    Assert.That((new FileInfo(filename)).Length > 0, Is.True);
  }

  [Test]
  public void TouchTest()
  {
    var touchDate = new DateTime(1984, 1, 1);

    FileUtils.Touch(_testStringsFile, touchDate);

    Assert.That(touchDate, Is.EqualTo(File.GetCreationTime(_testStringsFile)));
    Assert.That(touchDate, Is.EqualTo(File.GetLastAccessTime(_testStringsFile)));
    Assert.That(touchDate, Is.EqualTo(File.GetLastWriteTime(_testStringsFile)));
  }

  /* The Lines extension method lives in IEnumberable.cs.
     The method is tested here because it's convenient to do so. */
  [Test]
  public void LinesTest()
  {
    using (var sr = new StreamReader(_testStringsFile, true))
      Assert.That(sr.Lines().Count(), Is.EqualTo(10));
  }

  [Test]
  public void WriteMemoryStreamToFileTest()
  {
    var filename = this.GetTestFilename();

    using (var ms = new MemoryStream())
    {
      ms.Write(Encoding.UTF8.GetBytes(_testStringFoxAndDog), 0, _testStringFoxAndDog.Length);
      FileUtils.WriteMemoryStreamToFile(filename, ms);

      var contents = File.ReadAllText(filename);
      Assert.That(_testStringFoxAndDog, Is.EqualTo(contents));
    }
  }

  [Test]
  public void DeleteEmptyDirectoriesTest()
  {
    String rootDir = null;
    Assert.That(() => FileUtils.DeleteEmptyDirectories(rootDir), Throws.TypeOf<ArgumentNullException>());

    rootDir = "";
    Assert.That(() => FileUtils.DeleteEmptyDirectories(rootDir), Throws.TypeOf<ArgumentException>());

    rootDir = " ";
    Assert.That(() => FileUtils.DeleteEmptyDirectories(rootDir), Throws.TypeOf<ArgumentException>());

    rootDir = Path.Combine(_testFilesPath, "root");

    Directory.CreateDirectory(Path.Combine(rootDir, "empty/empty"));
    Directory.CreateDirectory(Path.Combine(rootDir, "empty/non empty"));
    Directory.CreateDirectory(Path.Combine(rootDir, "non empty/empty"));
    Directory.CreateDirectory(Path.Combine(rootDir, "non empty/non empty"));
    Directory.CreateDirectory(Path.Combine(rootDir, "really empty/empty"));
    Directory.CreateDirectory(Path.Combine(rootDir, "really empty/empty/empty"));

    File.WriteAllText(Path.Combine(rootDir, "empty/non empty/dummy.txt"), _testStringFoxAndDog);
    File.WriteAllText(Path.Combine(rootDir, "non empty/dummy.txt"), _testStringFoxAndDog);
    File.WriteAllText(Path.Combine(rootDir, "non empty/non empty/dummy.txt"), _testStringFoxAndDog);

    FileUtils.DeleteEmptyDirectories(rootDir);

    var areEmptyFoldersGone =
       Directory.Exists(Path.Combine(rootDir, "empty")) &&
      !Directory.Exists(Path.Combine(rootDir, "empty/empty")) &&
       Directory.Exists(Path.Combine(rootDir, "empty/non empty")) &&
      !Directory.Exists(Path.Combine(rootDir, "non empty/empty")) &&
       Directory.Exists(Path.Combine(rootDir, "non empty")) &&
       Directory.Exists(Path.Combine(rootDir, "non empty/non empty")) &&
      !Directory.Exists(Path.Combine(rootDir, "really empty/empty")) &&
      !Directory.Exists(Path.Combine(rootDir, "really empty/empty/empty"));

    Assert.That(areEmptyFoldersGone, Is.True);
  }

  [Test]
  public void IsDirectoryEmptyTest()
  {
    String rootDir = null;
    Assert.That(() => FileUtils.IsDirectoryEmpty(rootDir), Throws.TypeOf<ArgumentNullException>());

    rootDir = "";
    Assert.That(() => FileUtils.IsDirectoryEmpty(rootDir), Throws.TypeOf<ArgumentException>());

    rootDir = " ";
    Assert.That(() => FileUtils.IsDirectoryEmpty(rootDir), Throws.TypeOf<ArgumentException>());

    rootDir = Path.Combine(_testFilesPath, "root");

    Directory.CreateDirectory(Path.Combine(rootDir, "empty/empty"));
    Directory.CreateDirectory(Path.Combine(rootDir, "empty/non empty"));

    File.WriteAllText(Path.Combine(rootDir, "empty/non empty/dummy.txt"), _testStringFoxAndDog);

    Assert.That(FileUtils.IsDirectoryEmpty(Path.Combine(rootDir, "empty/empty")), Is.True);
    Assert.That(FileUtils.IsDirectoryEmpty(Path.Combine(rootDir, "empty/non empty")), Is.False);
  }

  /* GetMD5Checksum is tested indirectly via the MD5Checksum test in StringUtils.cs. */

  [Test]
  public void DuplicateBackslashesTest()
  {
    String directory = null;
    Assert.That(() => directory.DuplicateBackslashes(), Throws.TypeOf<ArgumentNullException>());

    directory = "";
    Assert.That(directory.DuplicateBackslashes() == "");

    directory = " ";
    Assert.That(directory.DuplicateBackslashes() == " ");

    directory = @"c:\\a\\\\b\\\\\\c\\\d";
    Assert.That(directory.DuplicateBackslashes() == @"c:\\a\\b\\c\\d");
  }

  [Test]
  public void AddTrailingSeparatorTest()
  {
    String directory = null;
    Assert.That(() => directory.AddTrailingSeparator(), Throws.TypeOf<ArgumentNullException>());

    directory = @"c:\temp";
    Assert.That(directory.AddTrailingSeparator() == directory + Path.DirectorySeparatorChar);

    directory = @"c:\temp\";
    Assert.That(directory.AddTrailingSeparator() == directory);
  }

  [Test]
  public void RemoveTrailingSeparatorTest()
  {
    String directory = null;
    Assert.That(() => directory.RemoveTrailingSeparator(), Throws.TypeOf<ArgumentNullException>());

    directory = @"c:\temp";
    Assert.That((directory + Path.DirectorySeparatorChar).RemoveTrailingSeparator() == directory);

    Assert.That(directory.RemoveTrailingSeparator() == directory);
  }

  [Test]
  public void AreFilenamesEqualTest()
  {
    Assert.That(FileUtils.AreFilenamesEqual(@"c:\dir1\dir2\file1.txt", @"c:\dir1\dir2\dir3\file1.txt"), Is.False);
    Assert.That(FileUtils.AreFilenamesEqual(@"c:\dir1\dir2\file1.txt", @"c:\dir1\dir2\dir3\..\file1.txt"), Is.True);
  }

  [Test]
  public void AreFilesEqualTest()
  {
    var filename1 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    var filename2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    try
    {
      /* Neither file exists yet. */
      Assert.That(() => FileUtils.AreFilesEqual(filename1, filename2), Throws.TypeOf<ArgumentException>());

      File.WriteAllText(filename1, "");

      /* Only one of the files exists. */
      Assert.That(() => FileUtils.AreFilesEqual(filename1, filename2), Throws.TypeOf<ArgumentException>());

      File.WriteAllText(filename2, "Hello, world!");

      /* Both files exist, but have different lengths and different contents. */
      Assert.That(FileUtils.AreFilesEqual(filename1, filename2), Is.False);

      File.WriteAllText(filename1, "abcdefghijklm");

      /* Both files exist, and have the same length, but have different contents. */
      Assert.That(FileUtils.AreFilesEqual(filename1, filename2), Is.False);

      File.WriteAllText(filename1, "");
      File.WriteAllText(filename2, "");

      /* Both files exist, have the same length, and are both empty. */
      Assert.That(FileUtils.AreFilesEqual(filename1, filename2), Is.True);

      File.WriteAllText(filename1, "Hello, world!");
      File.WriteAllText(filename2, "Hello, world!");

      /* Both files exist, have the same length, and have the same content. */
      Assert.That(FileUtils.AreFilesEqual(filename1, filename2), Is.True);
    }
    finally
    {
      File.Delete(filename1);
      File.Delete(filename2);
    }
  }
}

