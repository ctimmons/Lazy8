/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.IO;
using System.Reflection;
using System.Text;

using NUnit.Framework;

namespace Lazy8.Core.Tests;

public readonly record struct UUTestFilenames(String UnencodedDataFilename, String UUEncodedFilename, UUNullEncoding UUNullEncoding);

[TestFixture]
public class UUTests
{
  /* A post-build event copies the "UU Test Data" folder to this folder. */
  private static readonly String _dataPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "UU Test Data");

  private static void TestTextFile(UUTestFilenames uuTestFilenames)
  {
    var expectedUnencodedSource = File.ReadAllText(Path.Combine(_dataPath, uuTestFilenames.UnencodedDataFilename));
    var expectedUUEncodedSource = File.ReadAllText(Path.Combine(_dataPath, uuTestFilenames.UUEncodedFilename)).Trim().LF();
    var actualUUEncodedSource = UU.Encode(new UUData(uuTestFilenames.UnencodedDataFilename, Encoding.ASCII.GetBytes(expectedUnencodedSource)), uuTestFilenames.UUNullEncoding).Trim();
    Assert.That(actualUUEncodedSource, Is.EqualTo(expectedUUEncodedSource));

    var actualUnencodedSource = Encoding.ASCII.GetString(UU.Decode(actualUUEncodedSource).Contents);
    Assert.That(actualUnencodedSource, Is.EqualTo(expectedUnencodedSource));
  }

  private static void TestBinaryFile(UUTestFilenames uuTestFilenames)
  {
    var expectedUnencodedSource = File.ReadAllBytes(Path.Combine(_dataPath, uuTestFilenames.UnencodedDataFilename));
    var expectedUUEncodedSource = File.ReadAllText(Path.Combine(_dataPath, uuTestFilenames.UUEncodedFilename)).Trim().LF();
    var actualUUEncodedSource = UU.Encode(new UUData(uuTestFilenames.UnencodedDataFilename, expectedUnencodedSource), uuTestFilenames.UUNullEncoding).Trim();
    Assert.That(actualUUEncodedSource, Is.EqualTo(expectedUUEncodedSource));

    var actualUnencodedSource = UU.Decode(actualUUEncodedSource).Contents;
    Assert.That(actualUnencodedSource, Is.EqualTo(expectedUnencodedSource));
  }

  [Test]
  public void TestCatTextFile()
  {
    TestTextFile(new UUTestFilenames("cat.txt", "cat.uue", UUNullEncoding.UseSpaces));
  }

  [Test]
  public void TestImage_0BinaryFile()
  {
    TestBinaryFile(new UUTestFilenames("image_0.jpg", "image_0.uue", UUNullEncoding.UseSpaces));
  }

  [Test]
  public void TestUUTestBinaryFile()
  {
    TestBinaryFile(new UUTestFilenames("uutest.bin", "uutest.uue", UUNullEncoding.UseSpaces));
  }

  [Test]
  public void TestTrimmedLines()
  {
    /* Some lines in UU-encoded files may contain trailing space characters.
       This is a valid condition.  But these trailing spaces may get
       erroneously trimmed off by the various systems and/or editors
       the UU-encoded file passes thru, resulting in a corrupted UU-encoded file.

       The UU.Decode() method detects any missing trailing spaces and
       adds them back to the encoded line(s) before decoding.
    
       This test ensures the method actually does this by decoding a specially prepared
       file (uutest_trimmed.uue) and see if it decodes to the originally encoded
       source (uutest.bin). */

    var expectedUnencodedSource = File.ReadAllBytes(Path.Combine(_dataPath, "uutest.bin"));
    var uuEncodedSource = File.ReadAllText(Path.Combine(_dataPath, "uutest_trimmed.uue")).LF();
    var actualUnencodedSource = UU.Decode(uuEncodedSource!).Contents;
    Assert.That(actualUnencodedSource, Is.EqualTo(expectedUnencodedSource));
  }

  [Test]
  public void TestWithBackticksInsteadOfSpaces()
  {
    /* The original UU-encoding implementation on Unix encodes null bytes
       as a space character.  The space characters are subject to the problem
       noted above in the TestTrimmedLines() method.
    
       As there is no official UU-encoding standard, some enterprising programmers
       changed their implementations to encode null bytes as backticks (`) instead of spaces.
       This prevents the trailing-space-trimming problem, because there are
       no spaces in the encoded file.  However, now an obvious incompatibility
       issue crops up - the old "space-oriented" UU-decoders don't recognize backticks
       as valid encoded characters, and the new "backtick-oriented" UU-decoders
       don't recognize spaces as valid encoded characters.

       The UU.Decode() method decodes UU-encoded files which contain both spaces and backticks.
       Test this capability by decoding the uutest_backticks.uue file
       and comparing the output against the originally encoded uutest.bin file. */

    var expectedUnencodedSource = File.ReadAllBytes(Path.Combine(_dataPath, "uutest.bin"));
    var uuEncodedSource = File.ReadAllText(Path.Combine(_dataPath, "uutest_backticks.uue")).LF();
    var actualUnencodedSource = UU.Decode(uuEncodedSource!).Contents;
    Assert.That(actualUnencodedSource, Is.EqualTo(expectedUnencodedSource));
  }
}

