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
public class StringTests
{
  [Test]
  public void ToMemoryStreamTest()
  {
    String s = null;
    Assert.That(() => { using (var ms = s.ToMemoryStream()) { } }, Throws.TypeOf<ArgumentNullException>());

    s = "";
    using (var ms = s.ToMemoryStream())
      Assert.That(ms.Length == 0);

    s = "a";
    using (var ms = s.ToMemoryStream(Encoding.UTF8))
    {
      Assert.That(ms.Length == 1);
      using (var sr = new StreamReader(ms))
        Assert.That(sr.ReadToEnd() == s);
    }
  }

  [Test]
  public void RegexEscapeTest()
  {
    String s = null;
    Assert.That(() => s.RegexEscape(), Throws.TypeOf<ArgumentNullException>());

    s = "abc";
    Assert.That(Regex.Escape(s) == s.RegexEscape());

    s = @"ab\c";
    Assert.That(Regex.Escape(s) == s.RegexEscape());

    s = @"ab\c";
    Assert.That(s.RegexEscape('\\') == s);

    s = @"ab\.c";
    Assert.That(s.RegexEscape('\\') == @"ab\\.c");

    s = @"ab\.c";
    Assert.That(s.RegexEscape('\\', '.') == s);
  }

  [Test]
  public void GetRegexFromFilemaskTest()
  {
    String s = null;
    Assert.That(() => s.GetRegexFromFilemask(), Throws.TypeOf<ArgumentNullException>());

    s = "";
    Assert.That(() => s.GetRegexFromFilemask(), Throws.TypeOf<ArgumentException>());

    s = " ";
    Assert.That(() => s.GetRegexFromFilemask(), Throws.TypeOf<ArgumentException>());

    s = "abc";
    Assert.That(() => s.GetRegexFromFilemask(), Throws.TypeOf<ArgumentException>());

    s = "*";
    Assert.That(s.GetRegexFromFilemask().ToString() == "^.*?$");

    s = "?";
    Assert.That(s.GetRegexFromFilemask().ToString() == "^.$");

    s = "??";
    Assert.That(s.GetRegexFromFilemask().ToString() == "^..$");

    s = "a.b[cd]*";
    Assert.That(s.GetRegexFromFilemask().ToString() == @"^a\.b\[cd].*?$");
  }

  [Test]
  public void StripTest()
  {
    String s = null;
    Assert.That(() => s.Strip("x".ToCharArray()), Throws.TypeOf<ArgumentNullException>());

    s = "";
    Assert.That(() => s.Strip(null), Throws.TypeOf<ArgumentNullException>());

    s = "";
    Assert.That(s.Strip("x".ToCharArray()) == "");

    s = "";
    Assert.That(s.Strip("".ToCharArray()) == "");

    s = "abc";
    Assert.That(s.Strip("".ToCharArray()) == "abc");

    s = "abc";
    Assert.That(s.Strip("x".ToCharArray()) == "abc");

    s = "axbxcx";
    Assert.That(s.Strip("x".ToCharArray()) == "abc");
  }

  [Test]
  public void CoalesceTest()
  {
    Assert.That(() => StringUtils.Coalesce(null), Throws.TypeOf<ArgumentNullException>());
    Assert.That(() => StringUtils.Coalesce((String) null), Throws.TypeOf<ArgumentException>());

    Assert.That(() => StringUtils.Coalesce(), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce(""), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce("  "), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce(null, null), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce("", null), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce("  ", null), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce("  ", ""), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce("", "  "), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce("  ", "  "), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce("", ""), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce(null, ""), Throws.TypeOf<ArgumentException>());
    Assert.That(() => StringUtils.Coalesce(null, "  "), Throws.TypeOf<ArgumentException>());

    Assert.That(StringUtils.Coalesce("a") == "a");
    Assert.That(StringUtils.Coalesce("a", null) == "a");
    Assert.That(StringUtils.Coalesce(null, "a") == "a");
    Assert.That(StringUtils.Coalesce("a", "") == "a");
    Assert.That(StringUtils.Coalesce("", "a") == "a");
    Assert.That(StringUtils.Coalesce("a", "  ") == "a");
    Assert.That(StringUtils.Coalesce("  ", "a") == "a");
  }

  [Test]
  public void UpToTest()
  {
    String s = null;
    Assert.That(() => s.UpTo('.'), Throws.TypeOf<ArgumentNullException>());

    s = "";
    Assert.That(s.UpTo('.') == "");

    s = ".";
    Assert.That(s.UpTo('.') == "");

    s = ".abc";
    Assert.That(s.UpTo('.') == "");

    s = "abc.def";
    Assert.That(s.UpTo('.') == "abc");

    s = "abcdef.";
    Assert.That(s.UpTo('.') == "abcdef");

    s = "abcdef.";
    Assert.That(s.UpTo('x') == "abcdef.");
  }

  [Test]
  public void As0Or1Test()
  {
    Assert.That(false.As0Or1() == '0');
    Assert.That(true.As0Or1() == '1');
  }

  [Test]
  public void AsYOrNTest()
  {
    Assert.That(false.AsYOrN() == 'N');
    Assert.That(true.AsYOrN() == 'Y');
  }

  [Test]
  public void AsBooleanTest()
  {
    /* AsBoolean() calls ContainsCI(), which then calls IndexOfCI(), so there's no need to check null parameter behavior. */

    /* The strings '1', 'true', 't', 'yes' and 'y' should all return true (case insensitive). 
       Anything else should return false. */

    foreach (var trueInput in new[] { "1", "true", "TRUE", "t", "T", "yes", "YES", "y", "Y" })
      Assert.That(trueInput.AsBoolean(), Is.True);

    foreach (var falseInput in new[] { "", "0", "No", "False", "asfsadfdsf", null })
      Assert.That(falseInput.AsBoolean(), Is.False);
  }

  [Test]
  public void IndexOfCITest()
  {
    String s = null;
    Assert.That(() => s.IndexOfCI("x"), Throws.TypeOf<ArgumentNullException>());

    s = "abcdef";
    Assert.That(() => s.IndexOfCI(null), Throws.TypeOf<ArgumentNullException>());

    Assert.That(s.IndexOfCI("c") == 2);
    Assert.That(s.IndexOfCI("C") == 2);
    Assert.That(s.IndexOfCI("Z") == -1);
  }

  [Test]
  public void ContainsCITest()
  {
    /* ContainsCI() calls IndexOfCI(), so there's no need to check null parameter behavior. */

    var s = "abcdef";
    Assert.That(s.ContainsCI("c"), Is.True);
    Assert.That(s.ContainsCI("C"), Is.True);
    Assert.That(s.ContainsCI("Z"), Is.False);
  }

  [Test]
  public void EqualsCITest()
  {
    String foo;
    String bar;

    foo = null;
    bar = null;
    Assert.That(foo.EqualsCI(bar), Is.True);
    
    foo = "abcdef";
    bar = null;
    Assert.That(foo.EqualsCI(bar), Is.False);

    foo = null;
    bar = "abcdef";
    Assert.That(foo.EqualsCI(bar), Is.False);

    foo = "abcdef";
    bar = "abcdef";
    Assert.That(foo.EqualsCI(bar), Is.True);

    foo = "abcdef";
    bar = "zzzz";
    Assert.That(foo.EqualsCI(bar), Is.False);
  }

  [Test]
  public void EndsWithCITest()
  {
    String foo = null;
    Assert.That(() => foo.EndsWithCI(null), Throws.TypeOf<ArgumentNullException>());
    Assert.That(() => foo.EndsWithCI("ef"), Throws.TypeOf<ArgumentNullException>());

    foo = "abcdef";
    Assert.That(() => foo.EndsWithCI(null), Throws.TypeOf<ArgumentNullException>());

    Assert.That(foo.EndsWithCI("ef"), Is.True);
    Assert.That(foo.EndsWithCI("EF"), Is.True);
    Assert.That(foo.EndsWithCI("eZ"), Is.False);
  }

  [Test]
  public void StartsWithCITest()
  {
    String foo = null;
    Assert.That(() => foo.StartsWithCI(null), Throws.TypeOf<ArgumentNullException>());
    Assert.That(() => foo.StartsWithCI("ab"), Throws.TypeOf<ArgumentNullException>());

    foo = "abcdef";
    Assert.That(() => foo.StartsWithCI(null), Throws.TypeOf<ArgumentNullException>());

    Assert.That(foo.StartsWithCI("ab"), Is.True);
    Assert.That(foo.StartsWithCI("AB"), Is.True);
    Assert.That(foo.StartsWithCI("ZZ"), Is.False);
  }

  [Test]
  public void RepeatTest()
  {
    String s = null;
    var count = 1;
    Assert.That(() => s.Repeat(count), Throws.TypeOf<ArgumentNullException>());

    s = "";
    count = -1;
    Assert.That(() => s.Repeat(count), Throws.TypeOf<ArgumentOutOfRangeException>());

    s = "";
    count = 2;
    Assert.That(s.Repeat(count) == "", "A repeated empty string should be an empty string.");

    s = "a";
    count = 0;
    Assert.That(s.Repeat(count) == "");

    s = "a";
    count = 1;
    Assert.That(s.Repeat(count) == "a");

    s = "a";
    count = 2;
    Assert.That(s.Repeat(count) == "aa");

    s = "a";
    count = 10;
    Assert.That(s.Repeat(count) == "aaaaaaaaaa");
  }

  [Test]
  public void LastWordTest()
  {
    String s = null;
    Assert.That(() => s.LastWord(), Throws.TypeOf<ArgumentNullException>());

    s = "a";
    Assert.That(s.LastWord() == s);

    s = "a b";
    Assert.That(s.LastWord() == "b");

    s = "last   word   ";
    Assert.That(s.LastWord() == "word");
  }

  [Test]
  public void SurroundWithTest()
  {
    String s = null;
    String delimiter = null;
    Assert.That(() => s.SurroundWith(delimiter), Throws.TypeOf<ArgumentNullException>());

    s = "";
    Assert.That(() => s.SurroundWith(delimiter), Throws.TypeOf<ArgumentNullException>());

    s = null;
    delimiter = "";
    Assert.That(() => s.SurroundWith(delimiter), Throws.TypeOf<ArgumentNullException>());

    s = "";
    delimiter = "";
    Assert.That(s.SurroundWith(delimiter) == "");

    s = "";
    delimiter = "'";
    Assert.That(s.SurroundWith(delimiter) == "''");

    s = "a";
    delimiter = "'";
    Assert.That(s.SurroundWith(delimiter) == "'a'");

    var left = "abc";
    var right = "xyz";
    Assert.That(s.SurroundWith(left, right) == "abcaxyz");
  }

  /* Don't need to test the SingleQuote(), DoubleQuote(), or SquareBrackets() methods. 
     They're just specialized calls to SurroundWith(). */

  [Test]
  public void MD5ChecksumTest()
  {
    /* Correct test values from RFC 1321 (http://www.faqs.org/rfcs/rfc1321.html)

       MD5 ("") = d41d8cd98f00b204e9800998ecf8427e
       MD5 ("a") = 0cc175b9c0f1b6a831c399e269772661
       MD5 ("abc") = 900150983cd24fb0d6963f7d28e17f72
       MD5 ("message digest") = f96b697d7cb7938d525a2f31aaf161d0
       MD5 ("abcdefghijklmnopqrstuvwxyz") = c3fcd3d76192e4007dfb496cca67e13b
       MD5 ("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789") = d174ab98d277d9f5a5611c2c9f419d9f
       MD5 ("12345678901234567890123456789012345678901234567890123456789012345678901234567890") = 57edf4a22be3c955ac49da2e2107b67a */

    String input = null;
    Encoding encoding = null;
    Assert.That(() => input.MD5Checksum(encoding), Throws.TypeOf<ArgumentNullException>());

    input = "";
    Assert.That(() => input.MD5Checksum(encoding), Throws.TypeOf<ArgumentNullException>());

    input = null;
    encoding = Encoding.ASCII;
    Assert.That(() => input.MD5Checksum(encoding), Throws.TypeOf<ArgumentNullException>());

    input = "";
    Assert.That(input.MD5Checksum(encoding) == "D41D8CD98F00B204E9800998ECF8427E");

    input = "a";
    Assert.That(input.MD5Checksum(encoding) == "0CC175B9C0F1B6A831C399E269772661");

    input = "abc";
    Assert.That(input.MD5Checksum(encoding) == "900150983CD24FB0D6963F7D28E17F72");

    input = "message digest";
    Assert.That(input.MD5Checksum(encoding) == "F96B697D7CB7938D525A2F31AAF161D0");

    input = "abcdefghijklmnopqrstuvwxyz";
    Assert.That(input.MD5Checksum(encoding) == "C3FCD3D76192E4007DFB496CCA67E13B");

    input = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    Assert.That(input.MD5Checksum(encoding) == "D174AB98D277D9F5A5611C2C9F419D9F");

    input = "12345678901234567890123456789012345678901234567890123456789012345678901234567890";
    Assert.That(input.MD5Checksum(encoding) == "57EDF4A22BE3C955AC49DA2E2107B67A");
  }

  [Test]
  public void TrimStartTest()
  {
    String source = null;
    String stringToTrim = null;
    Assert.That(() => source.TrimStart(stringToTrim), Throws.TypeOf<ArgumentNullException>());

    source = "";
    Assert.That(() => source.TrimStart(stringToTrim), Throws.TypeOf<ArgumentNullException>());

    source = null;
    stringToTrim = "";
    Assert.That(() => source.TrimStart(stringToTrim), Throws.TypeOf<ArgumentNullException>());

    source = "";
    stringToTrim = "";
    Assert.That(source.TrimStart(stringToTrim) == "");

    source = "a";
    stringToTrim = "";
    Assert.That(source.TrimStart(stringToTrim) == "a");

    source = "aa";
    stringToTrim = "a";
    Assert.That(source.TrimStart(stringToTrim) == "a");

    source = "aa";
    stringToTrim = "aa";
    Assert.That(source.TrimStart(stringToTrim) == "");

    source = "aa";
    stringToTrim = "aaa";
    Assert.That(source.TrimStart(stringToTrim) == "aa");
  }

  [Test]
  public void TrimEndTest()
  {
    String source = null;
    String stringToTrim = null;
    Assert.That(() => source.TrimEnd(stringToTrim), Throws.TypeOf<ArgumentNullException>());

    source = "";
    Assert.That(() => source.TrimEnd(stringToTrim), Throws.TypeOf<ArgumentNullException>());

    source = null;
    stringToTrim = "";
    Assert.That(() => source.TrimEnd(stringToTrim), Throws.TypeOf<ArgumentNullException>());

    source = "";
    stringToTrim = "";
    Assert.That(source.TrimEnd(stringToTrim) == "");

    source = "a";
    stringToTrim = "";
    Assert.That(source.TrimEnd(stringToTrim) == "a");

    source = "aa";
    stringToTrim = "a";
    Assert.That(source.TrimEnd(stringToTrim) == "a");

    source = "aa";
    stringToTrim = "aa";
    Assert.That(source.TrimEnd(stringToTrim) == "");

    source = "aa";
    stringToTrim = "aaa";
    Assert.That(source.TrimEnd(stringToTrim) == "aa");
  }

  /* Don't need to test the RemovePrefixAndSuffix() method.
     It's just a combination of calls to TrimStart() and TrimEnd(). */

  [Test]
  public void AddTrailingForwardSlashTest()
  {
    String s = null;
    Assert.That(() => s.AddTrailingForwardSlash(), Throws.TypeOf<ArgumentNullException>());

    s = "";
    Assert.That(s.AddTrailingForwardSlash() == "/");

    s = "a";
    Assert.That(s.AddTrailingForwardSlash() == "a/");

    s = "a/";
    Assert.That(s.AddTrailingForwardSlash() == "a/");

    s = "a//";
    Assert.That(s.AddTrailingForwardSlash() == "a//");

    s = "a/ ";
    Assert.That(s.AddTrailingForwardSlash() == "a/ /");
  }

  [Test]
  public void RemoveHtmlTest()
  {
    String input = null;
    Assert.That(() => input.RemoveHtml(), Throws.TypeOf<ArgumentNullException>());

    input = "";
    Assert.That(input.RemoveHtml() == "");

    input = @"
        < html >
          <head>
            <  title >Hello, world!</title>
          </head>
          <body>
          </ body >
        </html>
        ";
    Assert.That(input.RemoveHtml().Trim() == "Hello, world!");
  }

  [Test]
  public void RemoveWhitespaceTest()
  {
    String input = null;
    Assert.That(() => input.RemoveWhitespace(), Throws.TypeOf<ArgumentNullException>());

    input = "";
    Assert.That(input.RemoveWhitespace() == "");

    /* A string of all possible whitespace characters in .Net. */
    input =
      String.Join("",
        Enumerable
          .Range(0, Convert.ToInt32(Char.MaxValue))
          .Select(c => Convert.ToChar(c))
          .Where(c => Char.IsControl(c) || Char.IsSeparator(c) || Char.IsWhiteSpace(c)));

    Assert.That(input.RemoveWhitespace() == "");
  }

  /* Don't need to test the IsNullOrWhiteSpace() and IsNotNullOrWhiteSpace() methods,
     because they're just fluent wrappers around System.String.IsNullOrWhiteSpace(). */

  [Test]
  public void AreAllEmptyTest()
  {
    List<String> strings = null;
    Assert.That(() => strings.AreAllEmpty(), Throws.TypeOf<ArgumentNullException>());

    strings = [];
    Assert.That(strings.AreAllEmpty(), Is.True);

    strings.Add("a");
    Assert.That(strings.AreAllEmpty(), Is.False);

    strings.Clear();
    strings.Add(" ");
    Assert.That(strings.AreAllEmpty(), Is.True);

    strings.Add("a");
    Assert.That(strings.AreAllEmpty(), Is.False);

    strings.Add("b");
    Assert.That(strings.AreAllEmpty(), Is.False);
  }

  [Test]
  public void AreAnyEmptyTest()
  {
    List<String> strings = null;
    Assert.That(() => strings.AreAnyEmpty(), Throws.TypeOf<ArgumentNullException>());

    strings = [];
    Assert.That(strings.AreAnyEmpty(), Is.False);

    strings.Add("a");
    Assert.That(strings.AreAnyEmpty(), Is.False);

    strings.Clear();
    strings.Add(" ");
    Assert.That(strings.AreAnyEmpty(), Is.True);

    strings.Add("a");
    Assert.That(strings.AreAnyEmpty(), Is.True);
  }

  [Test]
  public void IndentTest()
  {
    String input = null;
    Assert.That(() => input.Indent(4), Throws.TypeOf<ArgumentNullException>());

    input = "";
    Assert.That(input.Indent(4) == "    ");

    input = "one\ntwo\nthree\n";
    var output = "    one\n    two\n    three\n    ";

    Assert.That(input.Indent(0) == input);
    Assert.That(input.Indent(4) == output);

    input = output;
    output = "  one\n  two\n  three\n  ";
    Assert.That(input.Indent(-2) == output);

    input = output;
    Assert.That(input.Indent(-4) == output);
  }

  [Test]
  public void CRLFTest()
  {
    Assert.That(() => ((String) null).CRLF(), Throws.TypeOf<ArgumentNullException>());

    Assert.That("".CRLF() == "");

    Assert.That("\r".CRLF() == "\r\n");
    Assert.That("\n".CRLF() == "\r\n");
    Assert.That("\r\n".CRLF() == "\r\n");

    Assert.That("\r\r".CRLF() == "\r\n\r\n");
    Assert.That("\n\r".CRLF() == "\r\n\r\n");
    Assert.That("\n\n".CRLF() == "\r\n\r\n");
    Assert.That("\r\n\r\n".CRLF() == "\r\n\r\n");

    Assert.That("foo\r".CRLF() == "foo\r\n");
    Assert.That("foo\n".CRLF() == "foo\r\n");
    Assert.That("foo\r\n".CRLF() == "foo\r\n");

    Assert.That("\rfoo".CRLF() == "\r\nfoo");
    Assert.That("\nfoo".CRLF() == "\r\nfoo");
    Assert.That("\r\nfoo".CRLF() == "\r\nfoo");

    Assert.That("\rfoo\r".CRLF() == "\r\nfoo\r\n");
    Assert.That("\nfoo\n".CRLF() == "\r\nfoo\r\n");
    Assert.That("\r\nfoo\r\n".CRLF() == "\r\nfoo\r\n");
  }

  [Test]
  public void LFTest()
  {
    Assert.That(() => ((String) null).LF(), Throws.TypeOf<ArgumentNullException>());

    Assert.That("".LF() == "");

    Assert.That("\r".LF() == "\n");
    Assert.That("\n".LF() == "\n");
    Assert.That("\r\n".LF() == "\n");

    Assert.That("\r\r".LF() == "\n\n");
    Assert.That("\n\r".LF() == "\n\n");
    Assert.That("\n\n".LF() == "\n\n");
    Assert.That("\r\n\r\n".LF() == "\n\n");

    Assert.That("foo\r".LF() == "foo\n");
    Assert.That("foo\n".LF() == "foo\n");
    Assert.That("foo\r\n".LF() == "foo\n");

    Assert.That("\rfoo".LF() == "\nfoo");
    Assert.That("\nfoo".LF() == "\nfoo");
    Assert.That("\r\nfoo".LF() == "\nfoo");

    Assert.That("\rfoo\r".LF() == "\nfoo\n");
    Assert.That("\nfoo\n".LF() == "\nfoo\n");
    Assert.That("\r\nfoo\r\n".LF() == "\nfoo\n");
  }

  [Test]
  public void ChompTest()
  {
    Assert.That("".Chomp() == "");
    Assert.That("x".Chomp() == "x");
    Assert.That("xx".Chomp() == "xx");

    Assert.That("\n".Chomp() == "");
    Assert.That("x\n".Chomp() == "x");
    Assert.That("xx\n".Chomp() == "xx");

    Assert.That("\r".Chomp() == "");
    Assert.That("x\r".Chomp() == "x");
    Assert.That("xx\r".Chomp() == "xx");

    Assert.That("\r\n".Chomp() == "");
    Assert.That("x\r\n".Chomp() == "x");
    Assert.That("xx\r\n".Chomp() == "xx");

    Assert.That("\n\n".Chomp() == "\n");
    Assert.That("x\n\n".Chomp() == "x\n");
    Assert.That("xx\n\n".Chomp() == "xx\n");

    Assert.That("\r\r".Chomp() == "\r");
    Assert.That("x\r\r".Chomp() == "x\r");
    Assert.That("xx\r\r".Chomp() == "xx\r");

    Assert.That("\r\n\r\n".Chomp() == "\r\n");
    Assert.That("x\r\n\r\n".Chomp() == "x\r\n");
    Assert.That("xx\r\n\r\n".Chomp() == "xx\r\n");
  }

  [Test]
  public void GetSeriesTest()
  {
    Assert.That(() => ((String) null).GetSeries(), Throws.TypeOf<ArgumentNullException>());

    Assert.That("".GetSeries().ToList(), Is.EqualTo(new List<Int32>()));
    Assert.That("1".GetSeries().ToList(), Is.EqualTo(new List<Int32>() { 1 }));
    Assert.That("1, 2, 3".GetSeries().ToList(), Is.EqualTo(new List<Int32>() { 1, 2, 3 }));
    Assert.That("1-3".GetSeries().ToList(), Is.EqualTo(new List<Int32>() { 1, 2, 3 }));
    Assert.That("3, 2, 1, 6-8".GetSeries().ToList(), Is.EqualTo(new List<Int32>() { 3, 2, 1, 6, 7, 8 }));
    Assert.That("3, 2, 1, 6-8, 6-8".GetSeries().ToList(), Is.EqualTo(new List<Int32>() { 3, 2, 1, 6, 7, 8, 6, 7, 8 }));
    Assert.That("3, 2, 1, 6-8, 6-8".GetSeries(true).ToList(), Is.EqualTo(new List<Int32>() { 3, 2, 1, 6, 7, 8 }));
  }

  [Test]
  public void ChopBeginningAndEndTest()
  {
    Assert.That(() => ((String) null).ChopBeginningAndEnd(), Throws.TypeOf<ArgumentNullException>());

    var s = "";
    Assert.That(() => s.ChopBeginningAndEnd(), Throws.TypeOf<ArgumentOutOfRangeException>());

    s = "a";
    Assert.That(() => s.ChopBeginningAndEnd(), Throws.TypeOf<ArgumentOutOfRangeException>());

    s = "ab";
    Assert.That(s.ChopBeginningAndEnd() == "");

    s = "abc";
    Assert.That(s.ChopBeginningAndEnd() == "b");

    s = "123456789";
    Assert.That(s.ChopBeginningAndEnd(2) == "34567");
  }
}

