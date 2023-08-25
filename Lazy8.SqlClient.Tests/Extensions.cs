/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Data;
using System.Linq;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

using NUnit.Framework;

namespace Lazy8.SqlClient.Tests;

[TestFixture]
public class ExtensionsTests
{
  private static SqlConnection _connection;

  private const String _dropLazy8TestDatabaseSql = $@"
/* It is required to switch to a different database before dropping the Lazy8TestDB database.
   Otherwise SQL Server may consider Lazy8TestDB to be 'in use', and will throw an error. */

USE [master];
GO

DROP DATABASE IF EXISTS Lazy8TestDB;
GO
";

  private const String _createLazy8TestDatabaseSql = $@"
CREATE DATABASE Lazy8TestDB;
GO

USE Lazy8TestDB;
GO
";

  [SetUp]
  public void Init()
  {
    IConfiguration config =
      new ConfigurationBuilder()
      .AddJsonFile("appsettings.json")
      .Build();

    var connectionString = config.GetConnectionString("lazy8connectionstring");
    _connection = new SqlConnection(connectionString);
    _connection.Open();

    var sql = $@"
{_dropLazy8TestDatabaseSql}
{_createLazy8TestDatabaseSql}";

    _connection.ExecuteTSqlBatches(sql);
  }

  [TearDown]
  public void Cleanup()
  {
    /* Some of the methods being tested might have cloned _connection.
       When that happens, the cloned connection stays in the connection pool,
       even after it's Close() or Dispose() method is called.
       This causes the DROP DATABASE command in _dropLazy8TestDatabaseSql to fail,
       because the cloned connections are are still connected to the test database
       that's being dropped.

       (Note: if a pooled connection has its Close() or Dispose() method called,
       that doesn't really mean it's closed.  The pool will maintain a reference to
       the connection instance, and may even keep the connection open.  You can see
       this in action by opening a query window in SSMS and running 'exec sp_who'
       to see all active connections.)

       The solution is to remove all connections - cloned or otherwise - from the connection
       pool that were created using _connection's connection string.

       For more info on connection pooling, see:

         https://docs.microsoft.com/en-us/sql/connect/ado-net/sql-server-connection-pooling */

    SqlConnection.ClearPool(_connection);

    _connection.ExecuteTSqlBatches(_dropLazy8TestDatabaseSql);
    _connection.Close();
  }

  [Test]
  public void CheckConnectionStringTest()
  {
    /* Both well-formed and valid connection string. */
    Assert.That(() => _connection.ConnectionString.CheckConnectionString(), Throws.Nothing);

    /* Well-formed, but not valid. */
    SqlConnectionStringBuilder builder = new(_connection.ConnectionString) { DataSource = "database that does not exist" };
    Assert.That(() => builder.ConnectionString.CheckConnectionString(), Throws.TypeOf<SqlException>());

    /* Ill-formed. */
    Assert.That(() => "random text that can't possibly be a connection string".CheckConnectionString(), Throws.TypeOf<ArgumentException>());

    /* There is no test for both ill-formed and invalid. */
  }

  [Test]
  public void GetSchemaTablesTest()
  {
    var sql = @"
CREATE PROCEDURE dbo.Foo(@bar INT, @baz DATETIME)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT 'Foo', @bar;
  SELECT @baz;
END;";

    SqlParameter[] parameters =
      new[]
      {
        new SqlParameter() { ParameterName = "@bar", SqlDbType = SqlDbType.Int, Value = 42 },
        new SqlParameter() { ParameterName = "@baz", SqlDbType = SqlDbType.DateTime, Value = new DateTime(1999, 1, 1) }
      };

    _connection.ExecuteNonQuery(sql);
    var schemaTables = _connection.GetSchemaTables(CommandType.StoredProcedure, "dbo.Foo", parameters);

    /* All that really needs to be tested is how many schema tables were returned.
       Testing the contents and structure of the returned schema tables would just be 
       duplicating Microsoft.Data.SqlClient's unit tests. */

    Assert.That(schemaTables.Count() == 2);
  }

  private const String _createAndPopulateNullableTableSql = @$"
CREATE TABLE [dbo].[nullable_test]
(
  [id] [int] IDENTITY(1,1) NOT NULL,
  [nullable_bigint] [bigint] NULL,
  [nullable_bit] [bit] NULL,
  [nullable_decimal1] [decimal](5, 0) NULL,
  [nullable_decimal2] [decimal](10, 5) NULL,
  [nullable_int] [int] NULL,
  [nullable_money] [money] NULL,
  [nullable_smallint] [smallint] NULL,
  [nullable_smallmoney] [smallmoney] NULL,
  [nullable_tinyint] [tinyint] NULL,
  [nullable_float24] [real] NULL,
  [nullable_float53] [float] NULL,
  [nullable_date] [date] NULL,
  [nullable_datetime2] [datetime2](7) NULL,
  [nullable_datetime] [datetime] NULL,
  [nullable_datetimeoffset] [datetimeoffset](7) NULL,
  [nullable_smalldatetime] [smalldatetime] NULL,
  [nullable_time] [time](7) NULL,
  [nullable_char] [char](10) NULL,
  [nullable_varchar10] [varchar](10) NULL,
  [nullable_varchar_max] [varchar](max) NULL,
  [nullable_text] [text] NULL,
  [nullable_nchar] [nchar](10) NULL,
  [nullable_nvarchar10] [nvarchar](10) NULL,
  [nullable_nvarchar_max] [nvarchar](max) NULL,
  [nullable_ntext] [ntext] NULL,
  [nullable_binary] [binary](100) NULL,
  [nullable_varbinary100] [varbinary](100) NULL,
  [nullable_varbinary_max] [varbinary](max) NULL,
  [nullable_image] [image] NULL,
  [nullable_hierarchyid] [hierarchyid] NULL,
  [nullable_sql_variant] [sql_variant] NULL,
  [nullable_geometry] [geometry] NULL,
  [nullable_geography] [geography] NULL,
  [nullable_rowversion] [timestamp] NULL,
  [nullable_uniqueidentifier] [uniqueidentifier] NULL,
  [nullable_xml] [xml] NULL,
  PRIMARY KEY CLUSTERED 
  (
    [id] ASC
  ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

INSERT INTO [dbo].[nullable_test]
  (
    [nullable_bigint]
    ,[nullable_bit]
    ,[nullable_decimal1]
    ,[nullable_decimal2]
    ,[nullable_int]
    ,[nullable_money]
    ,[nullable_smallint]
    ,[nullable_smallmoney]
    ,[nullable_tinyint]
    ,[nullable_float24]
    ,[nullable_float53]
    ,[nullable_date]
    ,[nullable_datetime2]
    ,[nullable_datetime]
    ,[nullable_datetimeoffset]
    ,[nullable_smalldatetime]
    ,[nullable_time]
    ,[nullable_char]
    ,[nullable_varchar10]
    ,[nullable_varchar_max]
    ,[nullable_text]
    ,[nullable_nchar]
    ,[nullable_nvarchar10]
    ,[nullable_nvarchar_max]
    ,[nullable_ntext]
    ,[nullable_binary]
    ,[nullable_varbinary100]
    ,[nullable_varbinary_max]
    ,[nullable_image]
    ,[nullable_hierarchyid]
    ,[nullable_sql_variant]
    ,[nullable_geometry]
    ,[nullable_geography]
    ,[nullable_uniqueidentifier]
    ,[nullable_xml]
  )
  VALUES
  (
    42
    ,1
    ,42
    ,42.69
    ,42
    ,42.69
    ,42
    ,42
    ,42
    ,42.69
    ,42.69
    ,'1999-1-1'
    ,'1999-1-1'
    ,'1999-1-1'
    ,'1999-1-1 11:11:11 +01:00'
    ,'1999-1-1'
    ,'11:11:11'
    ,'frobozz'
    ,'frobozz'
    ,'frobozz'
    ,'frobozz'
    ,'frobozz'
    ,'frobozz'
    ,'frobozz'
    ,'frobozz'
    ,CAST(123456 AS BINARY(100))
    ,CAST(123456 AS VARBINARY(100))
    ,CAST(123456 AS VARBINARY(MAX))
    ,CAST(0x123456 AS IMAGE)
    ,hierarchyid::Parse('/1/2/3/')
    ,'frobozz'
    ,geometry::Parse('LINESTRING (100 100, 20 180, 180 180)')
    ,geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)')
    ,CAST('69696969-6969-6969-6969-696969696969' AS uniqueidentifier)
    ,'<root><items><item>Hello</item><item>World!</item></items></root>'
  ),
  (
    NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
    ,NULL
  );
GO";

  [Test]
  public void GetValueOrDefaultTest()
  {
    _connection.ExecuteTSqlBatches(_createAndPopulateNullableTableSql);

    using (var command = new SqlCommand() { CommandTimeout = 0, Connection = _connection, CommandType = CommandType.Text, CommandText = "SELECT * FROM dbo.nullable_test ORDER BY [id]" })
    {
      using (var reader = command.ExecuteReader())
      {
        /* There are two rows in the table.  Ordering by [id], the first row has data, which we'll call the "data row",
           and the second row has all NULL values, referred to as the "null row".
        
           Test the data row first. */

        reader.Read();
        reader.Read();

        Assert.That(reader.GetSqlInt64(reader.GetOrdinal("nullable_bigint")), Is.EqualTo(DBNull.Value));

        /* SQL Server's binary data type is a bit weird.
        
           Converting a value like 123456 to binary results in a byte array with the bytes in
           big-endian order, and the entire array is left-padded with zeros.

           This is in contrast to most "modern" languages which perform the opposite conversion (e.g. System.BitConverter):
           the bytes are in little-endian order and the array is right-padded with zeros.

           See:

             https://learn.microsoft.com/en-us/sql/t-sql/data-types/binary-and-varbinary-transact-sql */

        var expectedByteArray = BitConverter.GetBytes(123456); /* Little-endian byte order, right padded with zeros. */
        var actualByteArray = reader.GetFieldValue<Byte[]>("nullable_binary").Reverse().Take(4); /* Big-endian byte order, left padded with zeros.  Convert to little-endian. */
        Assert.That(actualByteArray.SequenceEqual(expectedByteArray), Is.True);

        Assert.That(reader.GetFieldValue<Boolean>("nullable_bit"), Is.True);
        Assert.That(reader.GetFieldValue<String>("nullable_char"), Is.EqualTo("frobozz".PadRight(10, ' ')));

        /*
          nonnullable_date = reader.GetValueOrDefault<System.DateTime>("[nonnullable_date]"),
          nonnullable_datetime = reader.GetValueOrDefault<System.DateTime>("[nonnullable_datetime]"),
          nonnullable_datetime2 = reader.GetValueOrDefault<System.DateTime>("[nonnullable_datetime2]"),
          nonnullable_datetimeoffset = reader.GetValueOrDefault<System.DateTimeOffset>("[nonnullable_datetimeoffset]"),
          nonnullable_decimal1 = reader.GetValueOrDefault<System.Double>("[nonnullable_decimal1]"),
          nonnullable_decimal2 = reader.GetValueOrDefault<System.Nullable<System.Double>>("[nonnullable_decimal2]"),
          nonnullable_float24 = reader.GetValueOrDefault<System.Single>("[nonnullable_float24]"),
          nonnullable_float53 = reader.GetValueOrDefault<System.Double>("[nonnullable_float53]"),
          nonnullable_geography = reader.GetValueOrDefault<Microsoft.SqlServer.Types.SqlGeography>("[nonnullable_geography]"),
          nonnullable_geometry = reader.GetValueOrDefault<Microsoft.SqlServer.Types.SqlGeometry>("[nonnullable_geometry]"),
          nonnullable_hierarchyid = reader.GetValueOrDefault<Microsoft.SqlServer.Types.SqlHierarchyId>("[nonnullable_hierarchyid]"),
          nonnullable_image = reader.GetValueOrDefault<System.Byte[]>("[nonnullable_image]"),
          nonnullable_int = reader.GetValueOrDefault<System.Int32>("[nonnullable_int]"),
          nonnullable_money = reader.GetValueOrDefault<System.Double>("[nonnullable_money]"),
          nonnullable_nchar = reader.GetValueOrDefault<System.String>("[nonnullable_nchar]"),
          nonnullable_ntext = reader.GetValueOrDefault<System.String>("[nonnullable_ntext]"),
          nonnullable_nvarchar_max = reader.GetValueOrDefault<System.String>("[nonnullable_nvarchar_max]"),
          nonnullable_nvarchar10 = reader.GetValueOrDefault<System.String>("[nonnullable_nvarchar10]"),
          nonnullable_rowversion = reader.GetValueOrDefault<System.Byte[]>("[nonnullable_rowversion]"),
          nonnullable_smalldatetime = reader.GetValueOrDefault<System.DateTime>("[nonnullable_smalldatetime]"),
          nonnullable_smallint = reader.GetValueOrDefault<System.Int16>("[nonnullable_smallint]"),
          nonnullable_smallmoney = reader.GetValueOrDefault<System.Double>("[nonnullable_smallmoney]"),
          nonnullable_sql_variant = reader.GetValueOrDefault<System.Object>("[nonnullable_sql_variant]"),
          nonnullable_text = reader.GetValueOrDefault<System.String>("[nonnullable_text]"),
          nonnullable_time = reader.GetValueOrDefault<System.TimeSpan>("[nonnullable_time]"),
          nonnullable_tinyint = reader.GetValueOrDefault<System.Byte>("[nonnullable_tinyint]"),
          nonnullable_uniqueidentifier = reader.GetValueOrDefault<System.Guid>("[nonnullable_uniqueidentifier]"),
          nonnullable_varbinary_max = reader.GetValueOrDefault<System.Byte[]>("[nonnullable_varbinary_max]"),
          nonnullable_varbinary100 = reader.GetValueOrDefault<System.Byte[]>("[nonnullable_varbinary100]"),
          nonnullable_varchar_max = reader.GetValueOrDefault<System.String>("[nonnullable_varchar_max]"),
          nonnullable_varchar10 = reader.GetValueOrDefault<System.String>("[nonnullable_varchar10]"),
          nonnullable_xml = reader.GetValueOrDefault<System.Xml.Linq.XElement>("[nonnullable_xml]")
        */
      }
    }
  }
}

