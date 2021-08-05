/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;

using NUnit.Framework;

namespace Lazy8.Core.Tests
{
  [TestFixture]
  public class ExceptionsTests
  {
    [Test]
    public void GetAllExceptionMessagesTest()
    {
      var result = "First Message\nSecond Message\nThird Message";
      var exception = new Exception("First Message", new Exception("Second Message", new Exception("Third Message")));
      Assert.That(result, Is.EqualTo(ExceptionUtils.GetAllExceptionMessages(exception)));
    }
  }
}
