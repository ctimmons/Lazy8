﻿/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using NUnit.Framework;

namespace Lazy8.Core.Tests;

[Serializable]
public class TestClass
{
  [XmlComment(@"
Multi-line comment.
Another line.
")]
  public String StringProperty1 { get; set; }

  [XmlComment(@"Single-line comment.")]
  public Int32 Int32Property1 { get; set; }

  [XmlComment(@"
Multi-line comment.
Another line.
")]
  public List<Int32> ListInt32Property1 { get; set; }

  public TestClass TestClassInstance { get; set; }

  public static TestClass GetInstance()
  {
    var testClass =
      new TestClass()
      {
        StringProperty1 = "Hello, world!",
        Int32Property1 = 42,
        ListInt32Property1 = [1, 2, 3],
        TestClassInstance =
          new TestClass()
          {
            StringProperty1 = "foo bar baz quux",
            Int32Property1 = 138,
            ListInt32Property1 = [4, 5, 6],
            TestClassInstance = null
          }
      };

    return testClass;
  }

  public static Boolean AreTestClassInstancesEqual(TestClass expected, TestClass actual)
  {
    return
      (expected.StringProperty1 == actual.StringProperty1) &&
      (expected.Int32Property1 == actual.Int32Property1) &&
      (expected.ListInt32Property1.Count == actual.ListInt32Property1.Count) &&
      Enumerable.SequenceEqual(expected.ListInt32Property1, actual.ListInt32Property1) &&
      (expected.TestClassInstance == null || actual.TestClassInstance == null || AreTestClassInstancesEqual(expected.TestClassInstance, actual.TestClassInstance));
  }
}

[TestFixture]
public partial class Xml
{
  public Xml() : base() { }

  [Test]
  public void GetXmlNamespacesTest()
  {
    var xml = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<xbrl
  xmlns=""http://www.xbrl.org/2003/instance""
  xmlns:dei=""http://xbrl.sec.gov/dei/2022""
  xmlns:xbrldi=""http://xbrl.org/2006/xbrldi"">
    <context id=""ibd1b0ecc6f554fd1a6cb7e4b9f834c12_D20230101-20230331"">
        <entity>
            <identifier scheme=""http://www.sec.gov/CIK"">0001004980</identifier>
            <segment>
                <xbrldi:explicitMember dimension=""dei:LegalEntityAxis"">pcg:PacificGasElectricCoMember</xbrldi:explicitMember>
            </segment>
        </entity>
        <period>
            <startDate>2023-01-01</startDate>
            <endDate>2023-03-31</endDate>
        </period>
    </context>
    <context id=""i914970c514674463b93d82e995e8edc6_D20230101-20230331"">
        <entity>
            <identifier scheme=""http://www.sec.gov/CIK"">0001004980</identifier>
            <segment>
                <xbrldi:explicitMember dimension=""us-gaap:StatementClassOfStockAxis"">pcg:CommonStockNoParValueMember</xbrldi:explicitMember>
                <xbrldi:explicitMember dimension=""dei:EntityListingsExchangeAxis"">exch:XNYS</xbrldi:explicitMember>
            </segment>
        </entity>
        <period>
            <startDate>2023-01-01</startDate>
            <endDate>2023-03-31</endDate>
        </period>
    </context>
</xbrl>
".Trim();

    var xmlNamespaces = XDocument.Parse(xml).GetXmlNamespaces();
    Assert.That(xmlNamespaces.Count(), Is.EqualTo(3));
    Assert.That(xmlNamespaces.Any(xmlNamespace => xmlNamespace.Prefix == "default" && xmlNamespace.Name == "http://www.xbrl.org/2003/instance"), Is.True);
    Assert.That(xmlNamespaces.Any(xmlNamespace => xmlNamespace.Prefix == "dei" && xmlNamespace.Name == "http://xbrl.sec.gov/dei/2022"), Is.True);
    Assert.That(xmlNamespaces.Any(xmlNamespace => xmlNamespace.Prefix == "xbrldi" && xmlNamespace.Name == "http://xbrl.org/2006/xbrldi"), Is.True);
  }

  [Test]
  public void XmlFileToFromTest()
  {
    var filename = Path.GetTempFileName();

    XmlUtils.SerializeObjectToXmlFile(TestClass.GetInstance(), filename);
    try
    {
      var testClass = XmlUtils.DeserializeObjectFromXmlFile<TestClass>(filename);
      Assert.That(TestClass.AreTestClassInstancesEqual(TestClass.GetInstance(), testClass), Is.True);
    }
    finally
    {
      File.Delete(filename);
    }
  }

  [Test]
  public void XDocumentToFromTest()
  {
    var xDocument = XmlUtils.SerializeObjectToXDocument(TestClass.GetInstance());
    var testClass = XmlUtils.DeserializeObjectFromXDocument<TestClass>(xDocument);
    Assert.That(TestClass.AreTestClassInstancesEqual(TestClass.GetInstance(), testClass), Is.True);
  }

  [Test]
  public void XmlStringToFromTest()
  {
    var s = XmlUtils.SerializeObjectToXmlString(TestClass.GetInstance());
    var testClass = XmlUtils.DeserializeObjectFromXmlString<TestClass>(s);
    Assert.That(TestClass.AreTestClassInstancesEqual(TestClass.GetInstance(), testClass), Is.True);
  }

  [Test]
  public void GetFormattedXmlTest()
  {
    var input = @"<test><element>value 1</element><element>value 2</element><element>value 3</element></test>";
    var expectedOutput =
@"<test>
  <element>value 1</element>
  <element>value 2</element>
  <element>value 3</element>
</test>";

    Assert.That(XmlUtils.GetFormattedXml(input).Replace("\r\n", "\n"), Is.EqualTo(expectedOutput));
  }

  private readonly XmlWriterSettings _xmlWriterSettings = new() { Indent = true, IndentChars = "  ", NewLineOnAttributes = true };

  [Test]
  public void WriteStartAndEndElementsTest()
  {
    var actual = new StringBuilder();
    var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Root>
  <Child>
    <Grandchild>text</Grandchild>
  </Child>
  <Child>
    <Grandchild>text</Grandchild>
  </Child>
</Root>";

    using (var writer = XmlWriter.Create(actual, this._xmlWriterSettings))
    {
      writer.WriteStartElements("Root", "Child", "Grandchild");
      writer.WriteString("text");
      writer.WriteEndElements(2);
      writer.WriteStartElements("Child", "Grandchild");
      writer.WriteString("text");
      writer.WriteEndElements(3);
      writer.Flush();

      Assert.That(actual.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
    }
  }

  [Test]
  public void WriteCDataElementTest()
  {
    var actual = new StringBuilder();
    var expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Root>
  <Child>
    <CDATA_Test
      type=""text""><![CDATA[<script>alert('Hello, world!');</script>]]></CDATA_Test>
  </Child>
</Root>";

    using (var writer = XmlWriter.Create(actual, this._xmlWriterSettings))
    {
      writer.WriteStartElements("Root", "Child");
      writer.WriteCDataElement("CDATA_Test", "<script>alert('Hello, world!');</script>");
      writer.WriteEndElements(2);
      writer.Flush();

      Assert.That(actual.ToString().Replace("\r\n", "\n"), Is.EqualTo(expected));
    }
  }

  [Test]
  public void GetLastChildsInnerTextTest()
  {
    var xmlText = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Root>
  <Child>first text</Child>
  <Child>middle text</Child>
  <Child>last text</Child>
</Root>";

    var xmlDoc = new XmlDocument();
    xmlDoc.LoadXml(xmlText);
    Assert.That(xmlDoc.GetLastChildsInnerText("/Root"), Is.EqualTo("last text"));
  }

  [Test]
  public void GetNodesInnerTextTest()
  {
    var xmlText = @"<?xml version=""1.0"" encoding=""utf-16""?>
<Root>
  <Child>first text</Child>
  <Child>middle text</Child>
  <Child>last text</Child>
</Root>";

    var xmlDoc = new XmlDocument();
    xmlDoc.LoadXml(xmlText);
    Assert.That(xmlDoc.GetNodesInnerText("/Root/Child[2]"), Is.EqualTo("middle text"));
  }

  [Test]
  public void XmlCommentFormattingTest()
  {
    var expected = @"<TestClass xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <!-- 
       Multi-line comment.
       Another line.
        -->
  <StringProperty1>Hello, world!</StringProperty1>
  <!-- Single-line comment. -->
  <Int32Property1>42</Int32Property1>
  <!-- 
       Multi-line comment.
       Another line.
        -->
  <ListInt32Property1>
    <int>1</int>
    <int>2</int>
    <int>3</int>
  </ListInt32Property1>
  <TestClassInstance>
    <!-- 
         Multi-line comment.
         Another line.
          -->
    <StringProperty1>foo bar baz quux</StringProperty1>
    <!-- Single-line comment. -->
    <Int32Property1>138</Int32Property1>
    <!-- 
         Multi-line comment.
         Another line.
          -->
    <ListInt32Property1>
      <int>4</int>
      <int>5</int>
      <int>6</int>
    </ListInt32Property1>
  </TestClassInstance>
</TestClass>";

    var actual = XmlUtils.SerializeObjectToXmlString(TestClass.GetInstance());

    /* The two XML strings, 'expected' and 'actual', cannot be directly compared like this:

         Assert.That(actual, Is.EqualTo(expected));

       This is because the serialization process inserts XML namespace attributes
       into the final XML string, and the order of the namespaces is not guaranteed
       nor predictable.

       Since all this test cares about are the XML comments, a regular expression
       and some LINQ magic are used to pluck out the comments and compare them. */

    var expectedXmlComments = XmlCommentRegex().Matches(expected).Cast<Match>().Select(m => m.Value);
    var actualXmlComments = XmlCommentRegex().Matches(actual).Cast<Match>().Select(m => m.Value.Replace("\r\n", "\n"));
    Assert.That(expectedXmlComments.SequenceEqual(actualXmlComments), Is.True);
  }

  [GeneratedRegex("\\<!--.*?--\\>", RegexOptions.Singleline)]
  private static partial Regex XmlCommentRegex();
}

