/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Data;

namespace Lazy8.SqlClient.Tests;

public static class DataTables
{
  public static DataTable GetItems()
  {
    DataTable result = new("item");

    DataColumn idColumn = new("id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(idColumn);
    result.PrimaryKey = [idColumn];

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

  public static DataTable GetCustomers()
  {
    DataTable result = new("customer");

    DataColumn idColumn = new("id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(idColumn);
    result.PrimaryKey = [idColumn];

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

  public static DataTable GetOrders()
  {
    DataTable result = new("order");

    DataColumn idColumn = new("id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(idColumn);
    result.PrimaryKey = [idColumn];

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

  public static DataTable GetOrderDetails()
  {
    DataTable result = new("order_detail");

    DataColumn idColumn = new("id", typeof(Int32)) { AllowDBNull = false };
    result.Columns.Add(idColumn);
    result.PrimaryKey = [idColumn];

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

  public static DataSet GetDataSet()
  {
    DataSet result = new();

    result.Tables.Add(GetItems());
    result.Tables.Add(GetCustomers());
    result.Tables.Add(GetOrders());
    result.Tables.Add(GetOrderDetails());

    result.Relations.Add(new DataRelation("Customer_Order", result.Tables["customer"]!.Columns["id"]!, result.Tables["order"]!.Columns["customer_id"]!));
    result.Relations.Add(new DataRelation("Item_OrderDetail", result.Tables["item"]!.Columns["id"]!, result.Tables["order_detail"]!.Columns["item_id"]!));
    result.Relations.Add(new DataRelation("Order_OrderDetail", result.Tables["order"]!.Columns["id"]!, result.Tables["order_detail"]!.Columns["order_id"]!));

    return result;
  }
}
