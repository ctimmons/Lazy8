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
public partial class Immutable_FileIO_Tests
{
  private static readonly String _testFilesPath = FileUtils.GetTemporarySubfolder();

  private static readonly String _level_1_1 = Path.Combine(_testFilesPath, @"level_1.1");
  private static readonly String _level_1_2 = Path.Combine(_testFilesPath, @"level_1.2");
  private static readonly String _level_2_2 = Path.Combine(_testFilesPath, @"level_1.1\level_2.2");
  private static readonly String _level_3_1 = Path.Combine(_testFilesPath, @"level_1.1\level_2.1\level_3.1");
  private static readonly String _level_3_2 = Path.Combine(_testFilesPath, @"level_1.1\level_2.1\level_3.2");

  private static readonly Int32 _totalNumberOfLevel_1Subdirectories = 6;
  private static readonly Int32 _totalNumberOfLevel_2Subdirectories = 4;
  private static readonly Int32 _totalNumberOfLevel_3Subdirectories = 2;

  public Immutable_FileIO_Tests()
    : base()
  {
  }

  [OneTimeSetUp]
  public void Init()
  {
    /* Set up an environment of folders and files that most of the unit tests use when they run.
       Note that this will result in 6 subdirectories under _testFilesPath. */

    Directory.CreateDirectory(_level_1_2);
    Directory.CreateDirectory(_level_2_2);
    Directory.CreateDirectory(_level_3_1);
    Directory.CreateDirectory(_level_3_2);

    /* None of the immutable file I/O tests examine the contents of files,
       so just write an empty string. */

    File.WriteAllText(Path.Combine(_level_1_1, "fox_and_dog.txt"), "");
    File.WriteAllText(Path.Combine(_level_1_2, "hello_world.txt"), "");
    File.WriteAllText(Path.Combine(_level_2_2, "hello_world.txt"), "");
    File.WriteAllText(Path.Combine(_level_3_1, "fox_and_dog.txt"), "");
    File.WriteAllText(Path.Combine(_level_3_2, "hello_world.txt"), "");
  }

  [OneTimeTearDown]
  public void Cleanup()
  {
    if (Directory.Exists(_testFilesPath))
      Directory.Delete(_testFilesPath, recursive: true);
  }

  [GeneratedRegex("_0", RegexOptions.Singleline)]
  private static partial Regex _level_0_NameRegex();

  [GeneratedRegex("_1", RegexOptions.Singleline)]
  private static partial Regex _level_1_NameRegex();

  [GeneratedRegex("_2", RegexOptions.Singleline)]
  private static partial Regex _level_2_NameRegex();

  [GeneratedRegex("_3", RegexOptions.Singleline)]
  private static partial Regex _level_3_NameRegex();

  [GeneratedRegex("_dog", RegexOptions.Singleline)]
  private static partial Regex _dog_NameRegex();

  [GeneratedRegex("_world", RegexOptions.Singleline)]
  private static partial Regex _world_NameRegex();

  [Test]
  public void GetDirectoriesTest()
  {
    var actual = FileUtils.GetDirectories(_testFilesPath, _level_1_NameRegex()).Length;
    var expected = _totalNumberOfLevel_1Subdirectories;

    Assert.That(actual == expected, Is.True);
  }
}
