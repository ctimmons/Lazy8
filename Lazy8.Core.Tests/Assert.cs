/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

namespace Lazy8.Core.Tests;

[TestFixture]
public class AssertTests
{
  [Test]
  public void NameTest()
  {
    var s = "Hello, world!";
    Assert.That(s.Name(nameof(s)).Name == "s");
  }

  [Test]
  public void NotOnlyWhitespaceTest()
  {
    var s = "Hello, world!";
    Assert.That(() => s.NotOnlyWhitespace(), Throws.Nothing);
    Assert.That(() => s.Name(nameof(s)).NotOnlyWhitespace(), Throws.Nothing);

    s = "  \t\r\n ";
    Assert.That(() => s.NotOnlyWhitespace(), Throws.TypeOf<ArgumentException>());
    Assert.That(() => s.Name(nameof(s)).NotOnlyWhitespace(), Throws.TypeOf<ArgumentException>());
  }

  [Test]
  public void NotNullTest()
  {
    var s = "Hello, world!";
    Assert.That(() => s.NotNull(), Throws.Nothing);
    Assert.That(() => s.Name(nameof(s)).NotNull(), Throws.Nothing);

    s = null;
    Assert.That(() => s.NotNull(), Throws.TypeOf<ArgumentNullException>());
    Assert.That(() => s.Name(nameof(s)).NotNull(), Throws.TypeOf<ArgumentNullException>());
  }

  [Test]
  public void NotEmptyTest()
  {
    var s = "Hello, world!";
    Assert.That(() => s.NotNull().NotEmpty(), Throws.Nothing);
    Assert.That(() => s.Name(nameof(s)).NotNull().NotEmpty(), Throws.Nothing);

    s = "";
    Assert.That(() => s.NotNull().NotEmpty(), Throws.TypeOf<ArgumentException>());
    Assert.That(() => s.Name(nameof(s)).NotNull().NotEmpty(), Throws.TypeOf<ArgumentException>());

    var lst = new List<Int32>() { 1, 2, 3 };
    Assert.That(() => lst.NotNull().NotEmpty(), Throws.Nothing);
    Assert.That(() => lst.Name(nameof(lst)).NotNull().NotEmpty(), Throws.Nothing);

    lst.Clear();
    Assert.That(() => lst.NotNull().NotEmpty(), Throws.TypeOf<ArgumentException>());
    Assert.That(() => lst.Name(nameof(lst)).NotNull().NotEmpty(), Throws.TypeOf<ArgumentException>());
  }

  [Test]
  public void GreaterThanTest()
  {
    var earlier = new DateTime(2014, 1, 1);
    var later = new DateTime(2016, 1, 1);
    Assert.That(() => later.GreaterThan(earlier), Throws.Nothing);
    Assert.That(() => later.Name(nameof(later)).GreaterThan(earlier), Throws.Nothing);
    Assert.That(() => earlier.GreaterThan(later), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => earlier.Name(nameof(earlier)).GreaterThan(later), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => earlier.GreaterThan(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => earlier.Name(nameof(earlier)).GreaterThan(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
  }

  [Test]
  public void GreaterThanOrEqualToTest()
  {
    var earlier = new DateTime(2014, 1, 1);
    var later = new DateTime(2016, 1, 1);
    Assert.That(() => later.GreaterThanOrEqualTo(earlier), Throws.Nothing);
    Assert.That(() => later.Name(nameof(later)).GreaterThanOrEqualTo(earlier), Throws.Nothing);
    Assert.That(() => earlier.GreaterThanOrEqualTo(later), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => earlier.Name(nameof(earlier)).GreaterThanOrEqualTo(later), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => earlier.GreaterThanOrEqualTo(earlier), Throws.Nothing);
    Assert.That(() => earlier.Name(nameof(earlier)).GreaterThanOrEqualTo(earlier), Throws.Nothing);
  }

  [Test]
  public void LessThanTest()
  {
    var earlier = new DateTime(2014, 1, 1);
    var later = new DateTime(2016, 1, 1);
    Assert.That(() => later.LessThan(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => later.Name(nameof(later)).LessThan(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => earlier.LessThan(later), Throws.Nothing);
    Assert.That(() => earlier.Name(nameof(earlier)).LessThan(later), Throws.Nothing);
    Assert.That(() => earlier.LessThan(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => earlier.Name(nameof(earlier)).LessThan(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
  }

  [Test]
  public void LessThanOrEqualToTest()
  {
    var earlier = new DateTime(2014, 1, 1);
    var later = new DateTime(2016, 1, 1);
    Assert.That(() => later.LessThanOrEqualTo(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => later.Name(nameof(later)).LessThanOrEqualTo(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => earlier.LessThanOrEqualTo(later), Throws.Nothing);
    Assert.That(() => earlier.Name(nameof(earlier)).LessThanOrEqualTo(later), Throws.Nothing);
    Assert.That(() => earlier.LessThanOrEqualTo(earlier), Throws.Nothing);
    Assert.That(() => earlier.Name(nameof(earlier)).LessThanOrEqualTo(earlier), Throws.Nothing);
  }

  [Test]
  public void EqualToTest()
  {
    var earlier = new DateTime(2014, 1, 1);
    var later = new DateTime(2016, 1, 1);
    Assert.That(() => later.EqualTo(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => later.Name(nameof(later)).EqualTo(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => earlier.EqualTo(earlier), Throws.Nothing);
    Assert.That(() => earlier.Name(nameof(earlier)).EqualTo(earlier), Throws.Nothing);
  }

  [Test]
  public void NotEqualToTest()
  {
    var earlier = new DateTime(2014, 1, 1);
    var later = new DateTime(2016, 1, 1);
    Assert.That(() => later.NotEqualTo(earlier), Throws.Nothing);
    Assert.That(() => later.Name(nameof(later)).NotEqualTo(earlier), Throws.Nothing);
    Assert.That(() => earlier.NotEqualTo(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => earlier.Name(nameof(earlier)).NotEqualTo(earlier), Throws.TypeOf<ArgumentOutOfRangeException>());
  }

  [Test]
  public void BetweenInclusiveTest()
  {
    var lowerBound = 5;
    var upperBound = 10;

    var value = 7;
    Assert.That(() => value.BetweenInclusive(lowerBound, upperBound), Throws.Nothing);
    Assert.That(() => value.Name(nameof(value)).BetweenInclusive(lowerBound, upperBound), Throws.Nothing);

    value = lowerBound;
    Assert.That(() => value.BetweenInclusive(lowerBound, upperBound), Throws.Nothing);
    Assert.That(() => value.Name(nameof(value)).BetweenInclusive(lowerBound, upperBound), Throws.Nothing);

    value = upperBound;
    Assert.That(() => value.BetweenInclusive(lowerBound, upperBound), Throws.Nothing);
    Assert.That(() => value.Name(nameof(value)).BetweenInclusive(lowerBound, upperBound), Throws.Nothing);

    value = 42;
    Assert.That(() => value.BetweenInclusive(lowerBound, upperBound), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => value.Name(nameof(value)).BetweenInclusive(lowerBound, upperBound), Throws.TypeOf<ArgumentOutOfRangeException>());
  }

  [Test]
  public void BetweenExclusiveTest()
  {
    var lowerBound = 5;
    var upperBound = 10;

    var value = 7;
    Assert.That(() => value.BetweenExclusive(lowerBound, upperBound), Throws.Nothing);
    Assert.That(() => value.Name(nameof(value)).BetweenExclusive(lowerBound, upperBound), Throws.Nothing);

    value = lowerBound;
    Assert.That(() => value.BetweenExclusive(lowerBound, upperBound), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => value.Name(nameof(value)).BetweenExclusive(lowerBound, upperBound), Throws.TypeOf<ArgumentOutOfRangeException>());

    value = upperBound;
    Assert.That(() => value.BetweenExclusive(lowerBound, upperBound), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => value.Name(nameof(value)).BetweenExclusive(lowerBound, upperBound), Throws.TypeOf<ArgumentOutOfRangeException>());

    value = 42;
    Assert.That(() => value.BetweenExclusive(lowerBound, upperBound), Throws.TypeOf<ArgumentOutOfRangeException>());
    Assert.That(() => value.Name(nameof(value)).BetweenExclusive(lowerBound, upperBound), Throws.TypeOf<ArgumentOutOfRangeException>());
  }

  [Test]
  public void DirectoryExistsTest()
  {
    var nameOfDirectoryThatExists = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    var nameOfDirectoryThatDoesNotExist = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    Directory.CreateDirectory(nameOfDirectoryThatExists);
    try
    {
      Assert.That(() => nameOfDirectoryThatExists.DirectoryExists(), Throws.Nothing);
      Assert.That(() => nameOfDirectoryThatExists.Name(nameof(nameOfDirectoryThatExists)).DirectoryExists(), Throws.Nothing);

      Assert.That(() => nameOfDirectoryThatDoesNotExist.DirectoryExists(), Throws.TypeOf<ArgumentException>());
      Assert.That(() => nameOfDirectoryThatDoesNotExist.Name(nameof(nameOfDirectoryThatExists)).DirectoryExists(), Throws.TypeOf<ArgumentException>());
    }
    finally
    {
      Directory.Delete(nameOfDirectoryThatExists);
    }
  }

  [Test]
  public void FileExistsTest()
  {
    var nameOfFileThatExists = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    var nameOfFileThatDoesNotExist = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    FileUtils.CreateEmptyFile(nameOfFileThatExists, OverwriteFile.Yes);
    try
    {
      Assert.That(() => nameOfFileThatExists.FileExists(), Throws.Nothing);
      Assert.That(() => nameOfFileThatExists.Name(nameof(nameOfFileThatExists)).FileExists(), Throws.Nothing);

      Assert.That(() => nameOfFileThatDoesNotExist.FileExists(), Throws.TypeOf<ArgumentException>());
      Assert.That(() => nameOfFileThatDoesNotExist.Name(nameof(nameOfFileThatExists)).FileExists(), Throws.TypeOf<ArgumentException>());
    }
    finally
    {
      File.Delete(nameOfFileThatExists);
    }
  }
}

