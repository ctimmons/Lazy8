/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;

using NUnit.Framework;

namespace Lazy8.Core.Tests
{
  [TestFixture]
  public class StringScannerTests
  {
    [Test]
    public void CreateInstanceTest()
    {
      Assert.That(() => new StringScanner(null!), Throws.TypeOf<ArgumentException>());
      Assert.That(() => new StringScanner(""), Throws.Nothing);
      Assert.That(() => new StringScanner("012"), Throws.Nothing);
    }

    [Test]
    public void EmptyInputTest()
    {
      /* Exercise StringScanner's reaction to an empty input string in one test method.
         This allows all of the other test methods to skip this kind of testing, resulting in
         reduced code clutter. */

      var s = new StringScanner("");

      Assert.That(s.IsBol, Is.True);
      Assert.That(s.IsEol, Is.True);
      Assert.That(s.Peek() == -1);
      Assert.That(s.ReversePeek() == -1);
      Assert.That(s.Read() == -1);
      Assert.That(s.MatchLiteral(""), Is.False);
      Assert.That(s.MatchLiteral("anything"), Is.False);
    }

    [Test]
    public void PeekTest()
    {
      var s = new StringScanner("012");
      Assert.That((Char) s.Peek() == '0');
      s.Read();
      Assert.That((Char) s.Peek() == '1');
      s.Read();
      Assert.That((Char) s.Peek() == '2');
      s.Read();
      Assert.That(s.Peek() == -1);
    }

    [Test]
    public void ReversePeekTest()
    {
      var s = new StringScanner("012");
      Assert.That(s.ReversePeek() == -1);
      s.Read();
      Assert.That((Char) s.ReversePeek() == '0');
      s.Read();
      Assert.That((Char) s.ReversePeek() == '1');
      s.Read();
      Assert.That((Char) s.ReversePeek() == '2');
    }

    [Test]
    public void ReadTest()
    {
      var s = new StringScanner("");
      Assert.That(s.Read() == -1);

      s = new StringScanner("012");
      Assert.That((Char) s.Read() == '0');
      Assert.That((Char) s.Read() == '1');
      Assert.That((Char) s.Read() == '2');
    }

    [Test]
    public void BolAndEolTest()
    {
      var s = new StringScanner("01");
      Assert.That(s.IsBol, Is.True);
      Assert.That(s.IsEol, Is.False);

      s.Read();
      Assert.That(s.IsBol, Is.False);
      Assert.That(s.IsEol, Is.False);

      s.Read();
      Assert.That(s.IsBol, Is.False);
      Assert.That(s.IsEol, Is.True);

      s = new StringScanner("\n\r0\n1\n");
      Assert.That(s.IsBol, Is.True);
      Assert.That(s.IsEol, Is.True);

      s.Read();
      Assert.That(s.IsBol, Is.True);
      Assert.That(s.IsEol, Is.True);

      s.Read();
      Assert.That(s.IsBol, Is.True);
      Assert.That(s.IsEol, Is.False);

      s.Read();
      Assert.That(s.IsBol, Is.False);
      Assert.That(s.IsEol, Is.True);

      s.Read();
      Assert.That(s.IsBol, Is.True);
      Assert.That(s.IsEol, Is.False);

      s.Read();
      Assert.That(s.IsBol, Is.False);
      Assert.That(s.IsEol, Is.True);

      s.Read();
      Assert.That(s.IsBol, Is.True);
      Assert.That(s.IsEol, Is.True);
    }

    /* There is no PredicateMatch unit test.  PredicateMatch is used by several other methods in StringScanner.
       If their unit tests pass, then PredicateMatch is assumed to pass too. */

    [Test]
    public void LiteralMatchTest()
    {
      var s = new StringScanner("go 1");
      Assert.That(s.MatchLiteral("GO"), Is.True);

      s = new StringScanner("01");
      Assert.That(s.MatchLiteral("0"), Is.True);
      Assert.That(s.MatchLiteral("1"), Is.True);
      Assert.That(s.MatchLiteral("1"), Is.False);

      s = new StringScanner("program foo");
      Assert.That(s.MatchLiteral("programming"), Is.False);
      Assert.That(s.MatchLiteral("program"), Is.True);
      s.SkipWhitespace();
      Assert.That(s.MatchLiteral("bar"), Is.False);
      Assert.That(s.MatchLiteral("foo"), Is.True);

      s = new StringScanner("program\nfoo\nbar baz");
      var pos = s.Position;
      Assert.That(s.MatchLiteral("program"), Is.True);
      Assert.That(pos == (1, 1));
      s.SkipLineEndings();

      pos = s.Position;
      Assert.That(s.MatchLiteral("foo"), Is.True);
      Assert.That(pos == (2, 1));
      s.SkipLineEndings();

      pos = s.Position;
      Assert.That(s.MatchLiteral("bar"), Is.True);
      Assert.That(pos == (3, 1));
      s.SkipLinearWhitespace();

      Assert.That(s.MatchLiteral("norf"), Is.False);

      pos = s.Position;
      Assert.That(s.MatchLiteral("baz"), Is.True);
      Assert.That(pos == (3, 5));
    }

    [Test]
    public void PositionTest_EmptyString()
    {
      /* StringScanner will accept an empty string to scan.
         It's harmless, as there's nothing that can really be done with an empty string.
         
         Note that even on an empty string the scanner's starting position is
         (line == 1) and (column == 1).  This matches the behavior of most text editors
         I've encountered, with emacs being the oddity - in fundamental mode, lines are
         1-based and columns are 0-based. */

      var s = new StringScanner("");
      var pos = s.Position;
      Assert.That(pos == (1, 1));
    }

    [Test]
    public void PositionTest_SingleLineString()
    {
      var s = new StringScanner("01");
      s.SavePosition();
      Assert.That((Char) s.Read() == '0');
      s.GoBackToSavedPosition();
      Assert.That(() => s.GoBackToSavedPosition(), Throws.TypeOf<Exception>());
      Assert.That(() => s.AcceptNewPosition(), Throws.TypeOf<Exception>());

      s.SavePosition();
      var pos = s.Position;
      Assert.That((Char) s.Read() == '0');
      Assert.That(pos == (1, 1));
      s.AcceptNewPosition();

      s.SavePosition();
      pos = s.Position;
      Assert.That((Char) s.Read() == '1');
      Assert.That(pos == (1, 2));
      s.AcceptNewPosition();
      Assert.That(() => s.AcceptNewPosition(), Throws.TypeOf<Exception>());
    }

    [Test]
    public void PositionTest_MultilineString()
    {
      var s = new StringScanner("\nfoo\nbar");

      s.SkipWhitespace();
      var pos = s.Position;
      Assert.That(pos == (2, 1));

      s.MatchLiteral("foo\nbar");
      pos = s.Position;
      Assert.That(pos == (3, 4));
    }

    [Test]
    public void MatchLineEndingsTest()
    {
      var s = new StringScanner("\n1\n23\n4");
      s.SkipLineEndings();
      Assert.That((Char) s.Read() == '1');
      s.SkipLineEndings();
      Assert.That((Char) s.Read() == '2');
      Assert.That((Char) s.Read() == '3');
      s.SkipLineEndings();
      Assert.That((Char) s.Read() == '4');
    }

    [Test]
    public void MatchLinearWhitespaceTest()
    {
      var s = new StringScanner(" 1 23 4");
      s.SkipLinearWhitespace();
      Assert.That((Char) s.Read() == '1');
      s.SkipLinearWhitespace();
      Assert.That((Char) s.Read() == '2');
      Assert.That((Char) s.Read() == '3');
      s.SkipLinearWhitespace();
      Assert.That((Char) s.Read() == '4');
    }

    [Test]
    public void MatchWhitespaceTest()
    {
      var s = new StringScanner("\n 1 \n 23\t\n 4");
      s.SkipWhitespace();
      Assert.That((Char) s.Read() == '1');
      s.SkipWhitespace();
      Assert.That((Char) s.Read() == '2');
      Assert.That((Char) s.Read() == '3');
      s.SkipWhitespace();
      Assert.That((Char) s.Read() == '4');
    }
  }
}
