/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lazy8.Core;

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

  //GetTSqlDml
  //ClearAllExcludedColumnFlags
  //ClearExcludedColumnFlag
  //SetExcludedColumnFlag
  //AddTSqlTypeInfoForStringColumns
  //GetTSqlDdl (datatable)
  //GetTSqlDdl (dataset)
}
