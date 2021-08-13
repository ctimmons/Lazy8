/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Data.SqlClient;

using Lazy8.Core;

namespace Lazy8.SqlClient
{
  /// <include file='GetTSqlBatchExtension.xml' path='docs/members[@name="sqlclient"]/GetTSqlBatchExtension/*'/>
  public static class GetTSqlBatchExtension
  {
    /// <include file='GetTSqlBatchExtension.xml' path='docs/members[@name="sqlclient"]/GetTSqlBatches/*'/>
    public static IEnumerable<String> GetTSqlBatches(this String tsql)
    {
      /* StringBuilder's default buffer size is 16 characters. It cannot be known in advance
         how large a single T-SQL batch will be, but 16 characters certainly won't be sufficient.
      
         To hopefully prevent StringBuilder from having to perform a large number of memory re-allocations,
         an arbitrary batch buffer size of 8K characters is selected.

         See StringBuilder's "Memory" entry in MSDN for more info:
      
           https://docs.microsoft.com/en-us/dotnet/api/system.text.stringbuilder?view=net-5.0#Memory */

      const Int32 kilobyte = 1024;
      StringBuilder batch = new(8 * kilobyte);

      StringScanner scanner = new(tsql);
      List<String> batches = new();

      while (scanner.Peek() != -1)
      {
        if (MatchSingleLineComment(scanner) || MatchNestedBlockComment(scanner))
          continue;

        var match = MatchStringLiteral(scanner);
        if (match.Trim().Any())
        {
          batch.Append(match);
          continue;
        }

        var goMultiplier = MatchGo(scanner);
        if (goMultiplier > 0)
        {
          batches.Add(GetBatch(batch.ToString(), goMultiplier));
          batch.Clear();
          continue;
        }

        batch.Append((Char) scanner.Read());
      }

      if (batch.Length > 0)
        batches.Add(GetBatch(batch.ToString(), 1));

      return batches.Where(b => b.Trim().Any());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="tsql"></param>
    public static void ExecuteTSqlBatches(this SqlConnection connection, String tsql)
    {
      using (var command = new SqlCommand() { Connection = connection, CommandType = CommandType.Text })
      {
        foreach (var batch in GetTSqlBatches(tsql))
        {
          command.CommandText = batch;
          command.ExecuteNonQuery();
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="filename"></param>
    public static void ExecuteTSqlFileBatches(this SqlConnection connection, String filename) => connection.ExecuteTSqlBatches(File.ReadAllText(filename));

    private static String GetBatch(String batch, Int32 goMultiplier)
    {
      var iteratedBatch = @$"
DECLARE @counter INT = 0;
WHILE @counter < {goMultiplier}
BEGIN
  {batch}
  SET @counter = @counter + 1;
END;
".Trim();

      return
        (goMultiplier == 1)
        ? batch
        : iteratedBatch;
    }

    private static String MatchNumber(StringScanner scanner)
    {
      StringBuilder result = new();

      var charValue = scanner.Peek();
      while (charValue != -1)
      {
        var ch = (Char) charValue;
        if (Char.IsDigit(ch))
          result.Append(ch);
        else
          break;

        scanner.Read();
        charValue = scanner.Peek();
      }

      return result.ToString();
    }

    private static Int32 MatchGo(StringScanner scanner)
    {
      /* The logic required to match a GO statement within a T-SQL script is non-trivial.
      
         T-SQL has a free-form syntax.  Its language constructs have little structure, and can appear pretty much anywhere
         the programmer wants to put them.

         Contrast that with the GO statement.  The SQL Server docs explicitly state the GO statement is NOT
         a part of T-SQL[0]:

           "GO is not a Transact-SQL statement; it is a command recognized by the sqlcmd and osql utilities
           and SQL Server Management Studio Code editor."

         And the GO statement has a restriction that T-SQL statements do not:

           "A Transact-SQL statement cannot occupy the same line as a GO command. However, the line can contain comments."

         [0]: https://docs.microsoft.com/en-us/sql/t-sql/language-elements/sql-server-utilities-statements-go?view=sql-server-ver15

         This last statement about comments is frustratingly vague.  Assuming a liberal interpretation, the documentation allows
         almost pathological syntax like this (C# doesn't allow nested block comments, so the code example below
         is presented using single-line // comments): */

      //   /* multiline
      //      block comment */    go /* block comment */42/*another multiline
      //      block comment*/

      /* This is the crux of the problem: even though the GO statement is not a part of T-SQL, any GO parser
         must be able to parse T-SQL comments and detect any legal GO statements occuring between such comments
         (including the GO statement's optional 'count' parameter).
      
         Fortunately, the main loop of this parser will handle leading and trailing block comments.
         Also, this code assumes valid inputs, so it won't check for illegal constructs like "go999", or "select 42; go".
         So this function only has to worry about valid, but non-trivial lines like this: */

      //         go /* block comment */42

      void SkipBlockCommentsAndLinearWhitespace()
      {
        scanner.SkipLinearWhitespace();
        while (MatchNestedBlockComment(scanner))
          scanner.SkipLinearWhitespace();
      }

      scanner.SavePosition();
      scanner.SkipLinearWhitespace();

      /* 'GO' and 'GOTO' are the only two keywords that start with 'GO'.
         Eliminate the chance of mistaking 'GOTO' for 'GO' by executing
         the following matches. */
      if (scanner.MatchLiteral("goto") || !scanner.MatchLiteral("go"))
      {
        scanner.GoBackToSavedPosition();
        return 0;
      }

      SkipBlockCommentsAndLinearWhitespace();

      if (MatchSingleLineComment(scanner))
      {
        scanner.AcceptNewPosition();
        return 1;
      }

      var count = MatchNumber(scanner);
      scanner.AcceptNewPosition();
      return count.Any() ? Convert.ToInt32(count) : 1;
    }

    private static String MatchStringLiteral(StringScanner scanner)
    {
      /* All T-SQL string literals can be delimited by single quotes.
         When SET QUOTED_IDENTIFIER OFF has been executed, string literals may also
         be delimited by double quotes.
      
         Why match string literals?

         String literals may contain character sequences that indicate the start or end of comments.
         If this matching function didn't exist, portions of those strings would be matched by either the
         MatchNestedBlockComment() or MatchSingleLineComment() methods, which would be incorrect logic. */

      const String SINGLE_QUOTE = "'";
      const String DOUBLE_QUOTE = "\"";

      String actualQuoteCharacter;
      if (scanner.MatchLiteral(SINGLE_QUOTE))
        actualQuoteCharacter = SINGLE_QUOTE;
      else if (scanner.MatchLiteral(DOUBLE_QUOTE))
        actualQuoteCharacter = DOUBLE_QUOTE;
      else
        return "";

      StringBuilder result = new(actualQuoteCharacter);

      /* A starting quote has already been matched, so start the
         string nesting level at one instead of zero. */
      var stringNestingLevel = 1;

      while ((stringNestingLevel > 0) && (scanner.Peek() != -1))
      {
        if (scanner.MatchLiteral(actualQuoteCharacter))
        {
          /* Is this an escape sequence? */
          if (scanner.MatchLiteral(actualQuoteCharacter))
            /* Yes. Add it to the result. */
            result.Append(actualQuoteCharacter);
          else
            /* No. It indicates the end of the string literal. */
            stringNestingLevel--;

          result.Append(actualQuoteCharacter);
        }
        else
        {
          result.Append((Char) scanner.Read());
        }
      }

      if (stringNestingLevel > 0)
        throw new Exception("Unbalanced string literal.");
      else
        return result.ToString();
    }

    private static Boolean MatchSingleLineComment(StringScanner scanner)
    {
      if (!scanner.MatchLiteral("--"))
        return false;

      while ((scanner.Peek() != -1) && ((Char) scanner.Peek() != '\r') && ((Char) scanner.Peek() != '\n'))
        scanner.Read();

      return true;
    }

    private static Boolean MatchNestedBlockComment(StringScanner scanner)
    {
      var blockCommentNestingLevel = 0;

      if (scanner.MatchLiteral("/*"))
        blockCommentNestingLevel++;
      else
        return false;

      while ((blockCommentNestingLevel > 0) && (scanner.Peek() != -1))
      {
        if (scanner.MatchLiteral("*/"))
          blockCommentNestingLevel--;
        else if (scanner.MatchLiteral("/*"))
          blockCommentNestingLevel++;
        else
          scanner.Read();
      }

      if (blockCommentNestingLevel > 0)
        throw new Exception("Unbalanced nested block comment.");
      else
        return true;
    }
  }
}
