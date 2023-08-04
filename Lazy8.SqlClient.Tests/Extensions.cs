﻿/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Data;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

using NUnit.Framework;

namespace Lazy8.SqlClient.Tests;

[TestFixture]
public class ExtensionsTests
{
  private static SqlConnection _connection;

  private static readonly DataSet _dataSet = GetDataSet();
  private static readonly DataTable _items = GetItems();
  private static readonly DataTable _customers = GetCustomers();
  private static readonly DataTable _orders = GetOrders();
  private static readonly DataTable _orderDetails = GetOrderDetails();

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

  private static DataTable GetItems()
  {
    DataTable result = new("item");

    DataColumn idColumn = new("id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(idColumn);
    result.PrimaryKey = new[] { idColumn };

    DataColumn descriptionColumn = new DataColumn("description", typeof(String)) { AllowDBNull = false };
    descriptionColumn.ExtendedProperties.Add("type", "nvarchar(100)");
    result.Columns.Add(descriptionColumn);

    var dataRow1 = result.NewRow();
    dataRow1["id"] = 1;
    dataRow1["description"] = "Foo";
    result.Rows.Add(dataRow1);

    var dataRow2 = result.NewRow();
    dataRow2["id"] = 2;
    dataRow2["description"] = "Bar";
    result.Rows.Add(dataRow2);

    var dataRow3 = result.NewRow();
    dataRow3["id"] = 3;
    dataRow3["description"] = "Baz";
    result.Rows.Add(dataRow3);

    var dataRow4 = result.NewRow();
    dataRow4["id"] = 4;
    dataRow4["description"] = "Quux";
    result.Rows.Add(dataRow4);

    var dataRow5 = result.NewRow();
    dataRow5["id"] = 5;
    dataRow5["description"] = "Norf";
    result.Rows.Add(dataRow5);

    return result;
  }

  private static DataTable GetCustomers()
  {
    DataTable result = new("customer");

    DataColumn idColumn = new("id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(idColumn);
    result.PrimaryKey = new[] { idColumn };

    DataColumn descriptionColumn = new DataColumn("name", typeof(String)) { AllowDBNull = false };
    descriptionColumn.ExtendedProperties.Add("type", "nvarchar(100)");
    result.Columns.Add(descriptionColumn);

    var dataRow1 = result.NewRow();
    dataRow1["id"] = 1;
    dataRow1["name"] = "Arthur Dent";
    result.Rows.Add(dataRow1);

    var dataRow2 = result.NewRow();
    dataRow2["id"] = 2;
    dataRow2["name"] = "Opus T. Penguin";
    result.Rows.Add(dataRow2);

    var dataRow3 = result.NewRow();
    dataRow3["id"] = 3;
    dataRow3["name"] = "Gyro Gearloose";
    result.Rows.Add(dataRow3);

    return result;
  }

  private static DataTable GetOrders()
  {
    DataTable result = new("order");

    DataColumn idColumn = new("id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(idColumn);
    result.PrimaryKey = new[] { idColumn };

    DataColumn customerIdColumn = new("customer_id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(customerIdColumn);

    DataColumn dateColumn = new DataColumn("date", typeof(DateTime)) { AllowDBNull = false };
    result.Columns.Add(dateColumn);

    var dataRow1 = result.NewRow();
    dataRow1["id"] = 1;
    dataRow1["customer_id"] = 1;
    dataRow1["date"] = new DateTime(2020, 1, 1);
    result.Rows.Add(dataRow1);

    var dataRow2 = result.NewRow();
    dataRow2["id"] = 2;
    dataRow2["customer_id"] = 2;
    dataRow2["date"] = new DateTime(2020, 1, 2);
    result.Rows.Add(dataRow2);

    var dataRow3 = result.NewRow();
    dataRow3["id"] = 3;
    dataRow3["customer_id"] = 3;
    dataRow3["date"] = new DateTime(2020, 1, 3);
    result.Rows.Add(dataRow3);

    return result;
  }

  private static DataTable GetOrderDetails()
  {
    DataTable result = new("order_detail");

    DataColumn idColumn = new("id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(idColumn);
    result.PrimaryKey = new[] { idColumn };

    DataColumn orderIdColumn = new("order_id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(orderIdColumn);

    DataColumn itemIdColumn = new DataColumn("item_id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(itemIdColumn);

    DataColumn priceColumn = new DataColumn("price", typeof(Double)) { AllowDBNull = false };
    result.Columns.Add(priceColumn);

    DataColumn quantityColumn = new DataColumn("quantity", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(quantityColumn);

    DataColumn lineTotalColumn = new DataColumn("line_total", typeof(Double)) { AllowDBNull = false, Expression = "price * quantity" };
    result.Columns.Add(lineTotalColumn);

    /* Order 1 */

    var dataRow1 = result.NewRow();
    dataRow1["id"] = 1;
    dataRow1["order_id"] = 1;
    dataRow1["item_id"] = 1;
    dataRow1["price"] = 1;
    dataRow1["quantity"] = 1;
    result.Rows.Add(dataRow1);

    /* Order 2 */

    var dataRow2 = result.NewRow();
    dataRow2["id"] = 2;
    dataRow2["order_id"] = 2;
    dataRow2["item_id"] = 1;
    dataRow2["price"] = 1;
    dataRow2["quantity"] = 1;
    result.Rows.Add(dataRow2);

    var dataRow3 = result.NewRow();
    dataRow3["id"] = 3;
    dataRow3["order_id"] = 2;
    dataRow3["item_id"] = 2;
    dataRow3["price"] = 2;
    dataRow3["quantity"] = 2;
    result.Rows.Add(dataRow3);

    /* Order 3 */

    var dataRow4 = result.NewRow();
    dataRow4["id"] = 4;
    dataRow4["order_id"] = 3;
    dataRow4["item_id"] = 1;
    dataRow4["price"] = 1;
    dataRow4["quantity"] = 1;
    result.Rows.Add(dataRow4);

    var dataRow5 = result.NewRow();
    dataRow5["id"] = 5;
    dataRow5["order_id"] = 3;
    dataRow5["item_id"] = 2;
    dataRow5["price"] = 2;
    dataRow5["quantity"] = 2;
    result.Rows.Add(dataRow5);

    var dataRow6 = result.NewRow();
    dataRow6["id"] = 6;
    dataRow6["order_id"] = 3;
    dataRow6["item_id"] = 3;
    dataRow6["price"] = 3;
    dataRow6["quantity"] = 3;
    result.Rows.Add(dataRow6);

    return result;
  }

  private static DataSet GetDataSet()
  {
    DataSet result = new();

    result.Tables.Add(GetItems());
    result.Tables.Add(GetCustomers());
    result.Tables.Add(GetOrders());
    result.Tables.Add(GetOrderDetails());

    result.Relations.Add(new DataRelation("Customer_Order", result.Tables["customer"].Columns["id"], result.Tables["order"].Columns["customer_id"]));
    result.Relations.Add(new DataRelation("Item_OrderDetail", result.Tables["item"].Columns["id"], result.Tables["order_detail"].Columns["item_id"]));
    result.Relations.Add(new DataRelation("Order_OrderDetail", result.Tables["order"].Columns["id"], result.Tables["order_detail"].Columns["order_id"]));

    return result;
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
  }
}

