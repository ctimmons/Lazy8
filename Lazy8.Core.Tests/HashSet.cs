/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;

using NUnit.Framework;

namespace Lazy8.Core.Tests;

[TestFixture]
public class HashSetTests
{
  [Test]
  public void GuardedHashSetTest()
  {
    /* Don't bother checking for a null predicate.
       Let the C# compiler's null checking option detect that
       condition at compile time.  In other words, the project file contains
       <Nullable>enable</Nullable> */
    //GuardedHashSet<String> foo = new(null);

    GuardedHashSet<String> foo = new(s => !String.IsNullOrWhiteSpace(s));
    Assert.That(foo.Add(""), Is.False);
    Assert.That(foo.Add(" "), Is.False);
    Assert.That(foo.Add("x"), Is.True);

    Assert.That(foo.Count == 1, Is.True);
    Assert.That(foo.Contains(""), Is.False);
    Assert.That(foo.Contains(" "), Is.False);
    Assert.That(foo.Contains("x"), Is.True);
  }
}

