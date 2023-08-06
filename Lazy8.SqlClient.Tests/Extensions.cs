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

  private static readonly DataSet _dataSet = DataTables.GetDataSet();
  private static readonly DataTable _items = DataTables.GetItems();
  private static readonly DataTable _customers = DataTables.GetCustomers();
  private static readonly DataTable _orders = DataTables.GetOrders();
  private static readonly DataTable _orderDetails = DataTables.GetOrderDetails();

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
{_createLazy8TestDatabaseSql}
{_dataSet.GetTSqlDdl()}";

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
}

