/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using NUnit.Framework;

namespace Lazy8.SqlClient.Tests;

public class TSqlBatchTest
{
  public TSqlBatch[]? Batches { get; set; }
}

public class GetTSqlBatchExtensionTests
{
  private static (String testCase, IEnumerable<TSqlBatch> expectedResults) GetTestCaseData(String filename)
  {
    /* Test cases are divided into sections, with a sequence of '--~' acting as a separator between sections.
       The first section contains the text of the T-SQL test case.  The second section contains
       a JSON array of the expected results (one or more T-SQL batches) after the test case
       has been split on its GO statements. */

    var contents = File.ReadAllText(filename).Split("--~", StringSplitOptions.RemoveEmptyEntries);
    return (contents[0], JsonSerializer.Deserialize<TSqlBatchTest>(contents[1])!.Batches!);
  }

  [Test]
  public void Test()
  {
    /* A post-build event copies the "Test Cases" folder to this assembly's folder,
       so the relative path to "Test Cases" used in Path.GetFullPath() works at runtime. */
    var testFilenames = Directory.GetFiles(Path.GetFullPath("Test Cases"), "*.txt", SearchOption.TopDirectoryOnly);

    foreach (var testCaseFilename in testFilenames)
    {
      (String testCase, IEnumerable<TSqlBatch> expectedResults) = GetTestCaseData(testCaseFilename);
      var actualResults = GetTSqlBatchExtension.GetTSqlBatches(testCase).Select(b => new TSqlBatch(b.Batch.Trim(), b.Count));
      Assert.That(actualResults, Is.EquivalentTo(expectedResults).IgnoreCase, Path.GetFileName(testCaseFilename));
    }
  }
}

