/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;

using NUnit.Framework;

namespace Lazy8.Core.Tests
{
  public class Test
  {
    public ConsoleModifiers ConsoleModifiers { get; set; }
  }

  [TestFixture]
  public class ReflectionUtilsTests
  {
    [Test]
    public void GetObjectInitializerTest()
    {
      var t = new Test() { ConsoleModifiers = ConsoleModifiers.Alt | ConsoleModifiers.Shift };
      Assert.That("new Test() { ConsoleModifiers = ConsoleModifiers.Alt | ConsoleModifiers.Shift }", Is.EqualTo(ReflectionUtils.GetObjectInitializer(t)));
    }
  }
}
