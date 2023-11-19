/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using NUnit.Framework;

namespace Lazy8.SqlClient.Tests;

[TestFixture]
public class GetTSqlDataTableExtensions
{
  [Test]
  public void AreDataTableContentsEqualTests()
  {
    var table1 = DataTables.GetItems();
    var table2 = DataTables.GetItems();
    var table3 = DataTables.GetCustomers();

    Assert.That(() => table1.AreDataTableContentsEqual(table2), Is.True);
    Assert.That(() => table1.AreDataTableContentsEqual(table3), Is.False);
  }

  //GetTSqlInsertStatements
  //ClearAllExcludedColumnFlags
  //ClearExcludedColumnFlag
  //SetExcludedColumnFlag
  //AddTSqlTypeInfoForStringColumns
  //GetTSqlDdl (datatable)
  //GetTSqlDdl (dataset)
}
