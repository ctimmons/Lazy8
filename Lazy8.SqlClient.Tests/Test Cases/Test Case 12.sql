  go
-- comment
/* comment */
label:
declare @s varchar(max) = '/* not a ''comment'' */';
goto label--comment
/*
go
*/
print '-- not a comment';
go
--~
label:
declare @s varchar(max) = '/* not a ''comment'' */';
goto label

print '-- not a comment';
