/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lazy8.Core;

public enum UUNullEncoding { UseSpaces, UseBackticks }

public readonly record struct UUData(String Filename, Byte[] Contents);

/*

The following paragraph was copied from https://wiki.tcl-lang.org/page/uuencode,
as it best describes the current state (and problems) of the uuencoding scheme.
The text is reproduced here under this license: http://tcl.tk/software/tcltk/license.html

"""
The uuencoding format has a troubled history. There is no formal specification for uuencoding,
and while it was a de-facto standard throughout the 90's, implementations have varied wildly
in realizing some of the format's subtleties. This includes variations in line lengths,
extra "parity" characters at the end of each line, and the encoding of '0' as either
the space character (in which case spaces at the end of a line may be truncated) or the tilde (sic)[0].
It gets worse when you consider possibilities for splitting uuencoded files across multiple messages.
"""

[0] Not 'tilde' - should be 'backtick' (`).

*/

public partial class UU
{
  private const Byte _lowerSixBitsMask = 0b111111;
  private const Byte _offset = 32;
  private const Byte _backtick = (Byte) '`';
  private const Int32 _maximumNumberOfBytesToEncodePerLine = 45;

  private static void DecodeLine(String line, MemoryStream ms)
  {
    /* Some encoded lines end with space characters.  Sometimes those trailing spaces get stripped off
       when the uuencoded data is passed thru various systems.  The trailing spaces are necessary
       to correctly decode the line.  The next two lines of code add trailing spaces if they're
       supposed to be there.

       First, determine the actual number of encoded characters (less the leading length character)
       that are supposed to be present in the line.  Then right-pad the line with spaces to the correct
       length.

       For example, a uuencoded data line consists of two parts: the first character contains
       the length (in bytes) of the *decoded* data, and the remaining characters contain the encoded data.

       The following line

         MLZ9-FQ8*A5Y__?4%"Q84%15=OGS9]HX'['5X>-BV!D"WTM)2>W,!E)65380

       has a physical length of 60, but it should be 61.  The trailing space is missing.

       Note the line starts with the character 'M', which contains the length of the decoded data as 45 bytes.
       This means there are 60 characters of encoded data.  The algorithm to extract the length stored in 'M' is:

         - 'M' is ASCII 77, which minus 32 gives 45 (number of bytes of the original decoded data in the line)
         - Multiplying 45 by 4/3 equals 60 (number of bytes of encoded data in the line)

       Therefore, the correct total length of this encoded line is the leading length
       character (1) plus 60 encoded characters, giving 61, not 60.

       To repair the line, it needs to be right-padded with spaces to a length of 61. */

    var encodedLength = (((Byte) (Encoding.ASCII.GetBytes(line[0].ToString())[0] - _offset) & _lowerSixBitsMask) * 4) / 3;
    line = line.PadRight(encodedLength + 1, ' ');

    /* The UU encoding scheme is kinda screwed up.

       There isn't an RFC or any other standard defining UU encoding, so
       various implementations have diverged, resulting in uuencoded files that
       some uudecoders don't recognize.

       One such point of divergence is the space character.  The original implementation
       allows space characters in uuencoded output.  One problem with this scheme is noted
       in the previous comment.  To fix this, other uuencoding implementations do not allow
       spaces in their output.  Instead, they use a backtick (ASCII 96).

       This prevents the problem noted above, but introduces incompatibility between uudecode
       implementations.  The following line of code simply converts all backticks to spaces
       before trying to decode a line. */

    var encodedBytes =
      Encoding
      .ASCII
      .GetBytes(line.Select(c => (c == _backtick) ? ' ' : c).ToArray())
      .Select(c => (Byte) ((c - _offset) & _lowerSixBitsMask))
      .ToArray();
    var decodedDataLength = encodedBytes[0];
    var numberOfDecodedDataBytesWritten = 0;

    /* The uudecoding algorithm works via bit shifting.  As such, it depends
       on unchecked overflows to operate correctly.

       Ensure the code always works by placing it within an 'unchecked' block,
       making the code immune to the 'Check for Arithmetic Overflow' property
       in the project's properties page (or the <CheckForOverflowUnderflow>
       tag in the *.csproj file.) */

    unchecked
    {
      for (var i = 1; i <= (encodedBytes.Length - 1); i += 4)
      {
        ms.WriteByte((Byte) ((encodedBytes[i] << 2) | (encodedBytes[i + 1] >> 4)));
        if (++numberOfDecodedDataBytesWritten == decodedDataLength)
          break;

        ms.WriteByte((Byte) ((encodedBytes[i + 1] << 4) | (encodedBytes[i + 2] >> 2)));
        if (++numberOfDecodedDataBytesWritten == decodedDataLength)
          break;

        ms.WriteByte((Byte) ((encodedBytes[i + 2] << 6) | encodedBytes[i + 3]));
        if (++numberOfDecodedDataBytesWritten == decodedDataLength)
          break;
      }
    }

    ms.Flush();
  }

  [GeneratedRegex(@"^begin\s+\d+\s+(?<filename>.+?)$", RegexOptions.Singleline)]
  private static partial Regex UuBeginHeaderRegex();

  private static String GetFilename(String line)
  {
    /* Use a regex to find the filename.

       Another technique would be to split 'line' on the space character,
       which should result in a three-element array.  The third element
       would nominally contain the filename.

       However, it's possible the filename contains spaces, making the 'splitting'
       approach subject to a bug where only the first part of the filename is extracted. */

    var match = UuBeginHeaderRegex().Match(line);
    if (!match.Success)
      throw new Exception($"'begin' line found, but does not appear to contain a filename.  Line = \n\n{line}");

    return match.Groups["filename"].Value.Trim();
  }

  public static UUData Decode(String uuEncodedText)
  {
    using (MemoryStream ms = new(uuEncodedText.Length))
    {
      var filename = "";

      using (StringReader sr = new(uuEncodedText.Trim()))
      {
        String line;

        /* Do not trim 'line', because trailing spaces are significant
           in the UU-encoding scheme. */
        while ((line = sr.ReadLine()) != null)
        {
          /* Some uuencoded files use a single space, or a bare /n to represent an empty line,
             while others use a backtick (`).  All should be ignored,
             along with the last line in the file - 'end'. */
          if (String.IsNullOrWhiteSpace(line) || (line == "`") || line.StartsWith("end"))
            continue;
          else if (line.StartsWith("begin"))
            filename = GetFilename(line);
          else
            DecodeLine(line, ms);
        }
      }

      ms.Flush();

      return new UUData(Filename: filename, Contents: ms.ToArray());
    }
  }

  private static void EncodeLine(Byte[] data, Int32 numberOfBytesToEncode, UUNullEncoding uuNullEncoding, StringBuilder sb)
  {
    Char getChar(Char c) => ((c == ' ') && (uuNullEncoding == UUNullEncoding.UseBackticks)) ? (Char) _backtick : c;

    /* Calculate and write length byte. */
    sb.Append(Convert.ToChar(numberOfBytesToEncode + _offset));

    for (var n = 0; n < numberOfBytesToEncode; n += 3)
    {
      if ((n + 3) <= numberOfBytesToEncode)
      {
        sb
        .Append(getChar(Convert.ToChar(((data[n] >> 2) & _lowerSixBitsMask) + _offset)))
        .Append(getChar(Convert.ToChar((((data[n] << 4) | (data[n + 1] >> 4)) & _lowerSixBitsMask) + _offset)))
        .Append(getChar(Convert.ToChar((((data[n + 1] << 2) | (data[n + 2] >> 6)) & _lowerSixBitsMask) + _offset)))
        .Append(getChar(Convert.ToChar((data[n + 2] & _lowerSixBitsMask) + _offset)));
      }
      else
      {
        if ((numberOfBytesToEncode % 3) == 1)
        {
          sb
          .Append(getChar(Convert.ToChar(((data[n] >> 2) & _lowerSixBitsMask) + _offset)))
          .Append(getChar(Convert.ToChar(((data[n] << 4) & _lowerSixBitsMask) + _offset)))
          .Append(getChar(' '))
          .Append(getChar(' '));
        }
        else
        {
          sb
          .Append(getChar(Convert.ToChar(((data[n] >> 2) & _lowerSixBitsMask) + _offset)))
          .Append(getChar(Convert.ToChar((((data[n] << 4) | (data[n + 1] >> 4)) & _lowerSixBitsMask) + _offset)))
          .Append(getChar(Convert.ToChar(((data[n + 1] << 2) & _lowerSixBitsMask) + _offset)))
          .Append(getChar(' '));
        }
      }
    }

    sb.Append('\n');
  }

  public static String Encode(UUData data, UUNullEncoding uuNullEncoding = UUNullEncoding.UseBackticks)
  {
    /* See https://en.wikipedia.org/wiki/File-system_permissions#Numeric_notation for a description
       of beginLine's mode (permissions) value. */

    var beginLine = $"begin 740 {data.Filename}\n";
    var endSignalLine = $"{((uuNullEncoding == UUNullEncoding.UseSpaces) ? ' ' : (Char) _backtick)}\n";
    var endLine = "end\n";

    /* Calculate how large the encoded string will be.
       Initialize StringBuilder to that capacity so it won't have to
       perform any memory reallocations while it's being populated with data. */

    var encodedDataLength = (data.Contents.Length * 4) / 3;
    var numberOfEncodedLines = (encodedDataLength / 60) + 1; /* Add one for a partial last line of encoded data. */
    var numberOfOverheadBytes = numberOfEncodedLines * 2 /* One length byte and one linefeed per line. */;
    var outputBufferSize = beginLine.Length + encodedDataLength + numberOfOverheadBytes + endSignalLine.Length + endLine.Length;

    StringBuilder result = new(outputBufferSize);

    /* Note: use StringBuilder's Append() method, not AppendLine(), to add data to the result.
       UU encoding requires a single \n (linefeed) at the end of each line.  AppendLine() appends
       the value of Environment.NewLine to the line, which will be \r\n (carriage return and
       linefeed pair) on Windows systems. */

    result.Append(beginLine);

    using (MemoryStream ms = new(data.Contents, writable: false))
    {
      var buffer = new Byte[_maximumNumberOfBytesToEncodePerLine];
      Int32 numberOfBytesRead;
      while ((numberOfBytesRead = ms.Read(buffer, 0, _maximumNumberOfBytesToEncodePerLine)) != 0)
        EncodeLine(buffer, numberOfBytesRead, uuNullEncoding, result);
    }

    result.Append(endSignalLine);
    result.Append(endLine);

    return result.ToString();
  }
}
