/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using NUnit.Framework;

namespace Lazy8.Core.Tests
{
  [TestFixture]
  public class IEnumerableTests
  {
    [Test]
    public void ContainsTest()
    {
      var values = new String[] { "one", "two", "three" };
      Assert.That(values.ContainsCI("one"), Is.True);
      Assert.That(values.ContainsCI("tw"), Is.False);
    }

    [Test]
    public void JoinTest()
    {
      var data = new List<String>() { "A", "B", "C", "D", "E" };
      var expected = "A, B, C, D, E";
      var actual = data.Join(", ");
      Assert.That(expected, Is.EqualTo(actual));
    }

    [Test]
    public void JoinAndTest()
    {
      var expected = "";
      var actual = (new[] { "" }).JoinAnd();
      Assert.That(expected, Is.EqualTo(actual), "Empty, comma.");

      actual = (new[] { "" }).JoinAnd(UseOxfordComma.No);
      Assert.That(expected, Is.EqualTo(actual), "Empty, no comma.");

      expected = "a";
      actual = (new[] { "a" }).JoinAnd();
      Assert.That(expected, Is.EqualTo(actual), "One element, comma.");

      expected = "a";
      actual = (new[] { "a" }).JoinAnd(UseOxfordComma.No);
      Assert.That(expected, Is.EqualTo(actual), "One element, no comma.");

      expected = "a and b";
      actual = (new[] { "a", "b" }).JoinAnd();
      Assert.That(expected, Is.EqualTo(actual), "Two elements, comma.");

      expected = "a and b";
      actual = (new[] { "a", "b" }).JoinAnd(UseOxfordComma.No);
      Assert.That(expected, Is.EqualTo(actual), "Two elements, no comma.");

      expected = "a, b, and c";
      actual = (new[] { "a", "b", "c" }).JoinAnd();
      Assert.That(expected, Is.EqualTo(actual), "Three elements, comma.");

      expected = "a, b and c";
      actual = (new[] { "a", "b", "c" }).JoinAnd(UseOxfordComma.No);
      Assert.That(expected, Is.EqualTo(actual), "Three elements, no comma.");
    }

    /* No point in writing a test for JoinOr().  It's the same code
       as JoinAnd(). */

    /* IEnumerableExtensions.Lines is tested in FileIO.cs. */

    [Test]
    public void IsNullOrEmptyTest()
    {
      var data = new List<String>() { "A", "B", "C", "D", "E" };
      var emptyData = new List<String>();

      Assert.That(data.IsNullOrEmpty(), Is.False);
      Assert.That(emptyData.IsNullOrEmpty(), Is.True);
      Assert.That(((IEnumerable<String>) null).IsNullOrEmpty(), Is.True);
    }

    [Test]
    public void ProductTest()
    {
      var data = new List<Int32>();
      BigInteger expected = 0;
      Assert.That(expected, Is.EqualTo(data.Product()));

      data = new List<Int32>() { 2, 3, 4, 5 };
      expected = 120;
      Assert.That(expected, Is.EqualTo(data.Product()));
    }

    [Test]
    public void SplitTest()
    {
      var data = new List<Int32>();
      var expected = new List<List<Int32>>();
      Assert.That(expected, Is.EqualTo(data.Split(3)));

      data.Add(1);
      expected.Add(new List<Int32>() { 1 });
      Assert.That(expected, Is.EqualTo(data.Split(3)));

      data.Add(2);
      expected.Add(new List<Int32>() { 2 });
      Assert.That(expected, Is.EqualTo(data.Split(3)));

      data.Add(3);
      expected.Add(new List<Int32>() { 3 });
      Assert.That(expected, Is.EqualTo(data.Split(3)));

      data.Add(4);
      expected.Add(new List<Int32>() { 4 });
      Assert.That(expected, Is.Not.EqualTo(data.Split(3)));

      expected =
        new List<List<Int32>>()
        {
          new List<Int32>() { 1, 4 },
          new List<Int32>() { 2 },
          new List<Int32>() { 3 }
        };
      Assert.That(expected, Is.EqualTo(data.Split(3)));
    }
    
    [Test]
    public void RandomizeInPlaceTest()
    {
      var data = new List<Int32>();
      Assert.That(data, Is.EqualTo(data.RandomizeInPlace())); // An empty list should equal itself.

      data.Add(1);
      data.Add(2);
      data.Add(3);
      data.Add(4);
      data.Add(5);
      Assert.That(data, Is.Not.EqualTo(data.ToList().RandomizeInPlace())); // Use .ToList() to get a clone of data.
    }
  }
}
