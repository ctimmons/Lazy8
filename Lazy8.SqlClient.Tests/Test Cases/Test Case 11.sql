SELECT 42;
go 3 -- go with count
SELECT 42;
--~
DECLARE @counter INT = 0;
WHILE @counter < 3
BEGIN
  SELECT 42;

  SET @counter = @counter + 1;
END;
--~
SELECT 42;
