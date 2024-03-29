### Test Case File Struture

A test case file contains two sections.  The first section contains the T-SQL code to test.  The second section contains the expected result(s) of a call to `GetTSqlBatchExtension.GetTSqlBatches(String)`.  The string contents of the tests are case-sensitive.

Sections are separated by a character sequence of two dashes and a tilde (--~), appearing on a line by itself.

The test case file can have any name, as long as it has a `.txt` file extension.

The test comparison logic is case-insensitive.

**Note**: `GetTSqlBatchExtension.GetTSqlBatches(String)` removes comments from the results it returns, but it does not remove the carriage returns and/or newlines after a comment.  This means the expected results in the test case file must this into account. See `Test Case 12.sql` for an example.



