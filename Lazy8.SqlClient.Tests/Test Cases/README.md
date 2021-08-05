### Test Case File Struture

A test case file contains two or more sections.  The first section contains the T-SQL code to test.  The second and subsequent sections contain the expected result(s) of a call to `GetTSqlBatchExtension.GetTSqlBatches(String)`.

Section are separated by a character sequence of two dashes and a tilde (--~), appearing on a line by itself.  This separator was chosen for two reasons.  First, the separator is a valid T-SQL comment, so the test case file can be loaded and edited in Visual Studio (VS) or SQL Server Management Studio (SSMS).  Second, it's highly unlikely that this particular sequence of characters would ever appear in normal T-SQL code.

The test case file can have any name, as long as it has a `.sql` file extension.

The test comparison logic is case-insensitive.

**Note**: `GetTSqlBatchExtension.GetTSqlBatches(String)` removes comments from the results it returns, but it does not remove the carriage returns and/or newlines after a comment.  This means the expected results in the test case file must this into account. See `Test Case 12.sql` for an example.



