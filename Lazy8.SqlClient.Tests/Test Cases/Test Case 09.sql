SELECT 42;
/* block comment with nested empty block comment (this is legal T-SQL), that contains a go statement
/**/
go
*/
SELECT 42;
--~
SELECT 42;

SELECT 42;
