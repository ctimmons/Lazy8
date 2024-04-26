## Fixing SQL Server's GO Statement

1. What's a GO Statement?

The GO statement is a batch separator. Technically it's not a part of T-SQL, but is recognized and acted upon by the
tools that process T-SQL, such as osql.exe, sqlcmd.exe, and SQL Server Management Studio (SSMS). GO can optionally have
a positive integer argument "N", which executes the T-SQL batch N times.  [More info here](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/sql-server-utilities-statements-go).

2. What's the Problem?

ProblemS, actually.

First, ADO.Net cannot execute T-SQL that contains GO statements.  There are at least three alternatives that can bypass ADO.Net and send GO-laden T-SQL statements to an SQL Server instance.  These alternatives are:

- The `TSqlParser` class, found in the `Microsoft.SqlServer.DacFx` NuGet package (parse only)
- SQL Server Management Objects (`SMO`), found in the `Microsoft.SqlServer.SqlManagementObject` NuGet package
- And, if it's installed, the `sqlcmd.exe` command line utility (`osql.exe` on SQL Server prior to version 2005)

The second problem is that, unfortunately, all three of these potential solutions have bugs.  There are too many to list here, so I
created a simple console app that displays all of the GO-related bugs I could find.  It's on GitHub in the
[ctimmons/cs_display_tsql_go_bugs](https://github.com/ctimmons/cs_display_tsql_go_bugs) repo.

Microsoft already knows about these bugs:

- [GO in 2nd half of nested block comments breaks batch parsing in SSMS and SQLCMD](https://feedback.azure.com/d365community/idea/59edae68-5325-ec11-b6e6-000d3a4f0da0)
- [Incorrect Syntax near 'GO' when nested block comments](https://feedback.azure.com/d365community/idea/76fed513-5725-ec11-b6e6-000d3a4f0da0)
- [SQL Server 2014 Management Studio, when delimiting on GO command, doesn't handle nested comments](https://feedback.azure.com/d365community/idea/5b100326-6c25-ec11-b6e6-000d3a4f0da0)

Note that these bugs are at least six years old at the time of this writing, and still they haven't been fixed.

3. Possible Solution

If pre-existing utilities and libraries can't handle the problem, it's time to turn to home-grown solutions.
Because T-SQL is just text, it may be tempting to use a regular expression (regex) to split the T-SQL code on GO
statements, resulting in a string array of T-SQL fragments that can each be individually executed.

Any programmer familiar with regular expressions will immediately see why this solution falls somewhere between
extremely difficult and impossible.  But I'll briefly list the reasons here so as to be complete.

First, the word GO can appear in comments and string literals.  Naively splitting on GO will
most likely result in the T-SQL string being split in incorrect places.  Second, if a regex is first used to remove
block comments, that regex can remove the wrong text because string literals can contain character sequences
like '/\*' and '*/'.  This would cause the comment-removal regex to remove the wrong text.

There are many other drawbacks to using regexes to parse T-SQL (or any non-linear data structure), but I'll stop here for the sake of brevity.

4. Actual Solution

The only bug-free solution I could think of was to create the GetTSqlBatchExtension static class with extension methods.  It implements a lexical scanner to perform a single pass of the T-SQL code, identifying and ignoring any comments it encounters.
When the scanner encounters a GO statement, the number of GO repetitions is determined (i.e. is the statement
"GO &lt;count&gt;", or just "GO"?). The T-SQL batch string and GO count are placed in a TSqlBatch record.

5. EBNF Rules for the Lexical Scanner

The rules reflect the fact that T-SQL allows nested multiline comments and string literals.

The EBNF syntax used here generally follows the description in [this Wikipedia page](https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form).

```
t-sql = { { go } | { string-literal } | { comment } | ? any character ? }

(* GO *)

go = ^ { multi-line-comment } "go" [ go-quantifier ] [ { multi-line-comment } | single_line_comment ] $

go-quantifier = { white-space } digit { digit }

white-space = ? white space characters ?

digit = "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"

(* STRING LITERALS *)

string-literal = double-quoted-string-literal | single-quoted-string-literal

double-quoted-string-literal = '"' { ? any character except double quote ? | ( '"' double-quoted-string-literal '"' ) } '"'

single-quoted-string-literal = "'" { ? any character except single quote ? | ( "'" single-quoted-string-literal "'" ) } "'"

(* COMMENTS *)

comment = single-line-comment | multi-line-comment

multi-line-comment = "/*" { no-star-slash | multi-line-comment } "*/"

no-star-slash = ? any character except the ordered pair of star and forward slash ?

single_line_comment = "--" { no-abstract-newline } abstract-newline

no-abstract-newline = ? any character except abstract-newline ?

(* Cover all of the bases regarding newline possibilities by using an abstraction over:
     
   - Windows (\r\n)
   - Unix-like and later versions of Windows (\n)
   - old MacOS (\r).

   Note that the abstract-newline rule could also be expressed as '( [ carriage-return ] newline ) | carriage-return'. *)

abstract-newline = ( carriage-return, newline ) | newline | carriage-return

carriage-return = '\r'

newline = '\n'
```