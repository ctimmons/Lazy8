/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.IO;

using Lazy8.Core;

using NUnit.Framework;

namespace Lazy8.SqlClient.Tests;

[TestFixture]
public class GetTSqlBatchExtensionTests
{
  private static (String testCase, IEnumerable<String> expectedResults) GetTestCaseData(String filename)
  {
    /* Test cases are divided into sections, with a sequence of '--~' indicating the border between sections.
       The first section contains the T-SQL test case.  Subsequent sections contain the expected results
       (one or more T-SQL batches) after the test case has been split on its GO statements. */

    var contents = File.ReadAllText(filename).Split("--~", StringSplitOptions.RemoveEmptyEntries);
    return (contents[0], contents[1..].Trim());
  }

  [Test]
  public void Test()
  {
    /* A post-build event copies the "Test Cases" folder to this assembly's folder,
       so the relative path to "Test Cases" used in Path.GetFullPath() works at runtime. */
    foreach (var testCaseFilename in Directory.GetFiles(Path.GetFullPath("Test Cases"), "*.sql", SearchOption.TopDirectoryOnly))
    {
      (String testCase, IEnumerable<String> expectedResults) = GetTestCaseData(testCaseFilename);
      var actualResults = GetTSqlBatchExtension.GetTSqlBatches(testCase).Trim();
      Assert.That(actualResults, Is.EqualTo(expectedResults).IgnoreCase, Path.GetFileName(testCaseFilename));
    }
  }
}

