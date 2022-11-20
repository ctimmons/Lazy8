/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Lazy8.Core;

public static class XmlUtils
{
  /// <summary>
  /// Given a <see cref="String"/> containing an XML document, return a string containing a formatted version of the input XML document string.
  /// </summary>
  /// <param name="xml">A <see cref="String"/> containing an XML document.</param>
  /// <returns>A <see cref="String"/> containing a formatted version of the input XML document string.</returns>
  public static String GetFormattedXml(String xml)
  {
    using (var sw = new StringWriter())
    {
      using (var xmlWriter = new XmlTextWriter(sw))
      {
        xmlWriter.Formatting = Formatting.Indented;

        var xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xml);
        xmlDocument.WriteTo(xmlWriter);

        return sw.ToString();
      }
    }
  }

  /// <summary>
  /// Given a <paramref name="value"/> of type <typeparamref name="T"/>, format it as an XML document and store it in <paramref name="filename"/>.
  /// </summary>
  /// <typeparam name="T">Any type.</typeparam>
  /// <param name="value">An object of type <typeparamref name="T"/>.</param>
  /// <param name="filename">A <see cref="String"/> containing a filename.</param>
  public static void SerializeObjectToXmlFile<T>(T value, String filename) => SerializeObjectToXDocument(value).Save(filename);

  /// <summary>
  /// Deserialize and return the XML document of type <typeparamref name="T"/> that's stored in <paramref name="filename"/>.
  /// </summary>
  /// <typeparam name="T">Any type.</typeparam>
  /// <param name="filename">A <see cref="String"/> containing an existing filename.</param>
  /// <returns>An object of type <typeparamref name="T"/>.</returns>
  public static T DeserializeObjectFromXmlFile<T>(String filename)
  {
    using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
      return (T) (new XmlSerializer(typeof(T))).Deserialize(fs);
  }

  /// <summary>
  /// Given a <paramref name="value"/> of type <typeparamref name="T"/>, convert it to an <see cref="XDocument"/>.
  /// </summary>
  /// <typeparam name="T">Any type.</typeparam>
  /// <param name="value">An object of type <typeparamref name="T"/>.</param>
  /// <returns>An <see cref="XDocument"/>.</returns>
  public static XDocument SerializeObjectToXDocument<T>(T value)
  {
    var doc = new XDocument();
    using (var writer = doc.CreateWriter())
      (new XmlSerializer(typeof(T))).Serialize(writer, value);

    /* Any XmlCommentAttributes in 'value' are *not* automatically inserted into the
       XDocument result 'doc' by the call to Serialize().

       The comments have to be inserted by a separate call to InsertXmlComments(). */

    /* This must be executed after writer has been closed. */
    InsertXmlComments(value, doc.Elements(), 1);

    return doc;
  }

  /// <summary>
  /// Given an <see cref="XDocument"/> instance that contains a serialized object, deserialize and return that object.
  /// </summary>
  /// <typeparam name="T">Any type.</typeparam>
  /// <param name="value">An <see cref="XDocument"/> instance.</param>
  /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
  public static T DeserializeObjectFromXDocument<T>(XDocument value)
  {
    var doc = new XDocument(value);
    using (var reader = doc.CreateReader())
      return (T) (new XmlSerializer(typeof(T))).Deserialize(reader);
  }

  /// <summary>
  /// Given a <paramref name="value"/> of type <typeparamref name="T"/>, convert it to an XML string.
  /// </summary>
  /// <typeparam name="T">Any type.</typeparam>
  /// <param name="value">An object of type <typeparamref name="T"/>.</param>
  /// <returns>An XML document in the form of a <see cref="String"/>.</returns>
  public static String SerializeObjectToXmlString<T>(T value) => SerializeObjectToXDocument(value).ToString();

  /// <summary>
  /// Given an XML string <paramref name="s"/> which contains an object of type <typeparamref name="T"/>,
  /// deserialize and return that object.
  /// </summary>
  /// <typeparam name="T">Any type.</typeparam>
  /// <param name="s">A <see cref="String"/>.</param>
  /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
  public static T DeserializeObjectFromXmlString<T>(String s)
  {
    using (var sr = new StringReader(s))
      return (T) (new XmlSerializer(typeof(T))).Deserialize(sr);
  }

  private static readonly Type _xmlCommentAttributeType = typeof(XmlCommentAttribute);
  private static readonly Boolean _shouldSearchInheritanceChain = false;

  /* Any XmlCommentAttributes in obj are inserted into xElements as XML comments (XComment objects).

     This method assumes xElements is a serialization of obj.
     If that's not the case, the behavior of this method is unpredictable. */
  private static void InsertXmlComments(Object obj, IEnumerable<XElement> xElements, Int32 level)
  {
    /* Base case. */
    if ((obj == null) || !xElements.Any())
      return;

    XComment getXCommentWithIndentedText(XmlCommentAttribute xmlCommentAttribute)
    {
      if (xmlCommentAttribute.ShouldIndent)
      {
        var prefixSpaces = " ".Repeat((level * xmlCommentAttribute.IndentSize) + "<!-- ".Length);
        return new XComment(xmlCommentAttribute.Value.Replace("\n", $"\n{prefixSpaces}"));
      }
      else
      {
        return new XComment(xmlCommentAttribute.Value);
      }
    }

    /* Recursive case. */
    foreach (var propertyInfo in obj.GetType().GetProperties())
    {
      if (propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0)
      {
        if (propertyInfo.IsDefined(_xmlCommentAttributeType, _shouldSearchInheritanceChain))
        {
          var xmlComment =
            propertyInfo
            .GetCustomAttributes(_xmlCommentAttributeType, _shouldSearchInheritanceChain)
            .Cast<XmlCommentAttribute>()
            .Single();

          xElements
          .Elements(propertyInfo.Name)
          .Single()
          .AddBeforeSelf(getXCommentWithIndentedText(xmlComment));
        }

        InsertXmlComments(propertyInfo.GetValue(obj, null), xElements.Elements(propertyInfo.Name), level + 1);
      }
    }
  }

  /// <summary>
  /// Write multiple start elements to <paramref name="xmlWriter"/>.
  /// </summary>
  /// <param name="xmlWriter">An <see cref="XmlWriter"/>.</param>
  /// <param name="tagNames">Zero or more strings.</param>
  public static void WriteStartElements(this XmlWriter xmlWriter, params String[] tagNames)
  {
    foreach (var tagName in tagNames)
      xmlWriter.WriteStartElement(tagName);
  }

  /// <summary>
  /// Write multiple end elements to <paramref name="xmlWriter"/>.
  /// </summary>
  /// <param name="xmlWriter">An <see cref="XmlWriter"/>.</param>
  /// <param name="count">An <see cref="Int32"/> greater than or equal to zero.</param>
  public static void WriteEndElements(this XmlWriter xmlWriter, Int32 count)
  {
    for (var i = 0; i < count; i++)
      xmlWriter.WriteEndElement();
  }

  /// <summary>
  /// Write a CDATA element to <paramref name="xmlWriter"/>.
  /// </summary>
  /// <param name="xmlWriter">An <see cref="XmlWriter"/>.</param>
  /// <param name="elementName">The element name to hold the CDATA.</param>
  /// <param name="cData">The CDATA to write.</param>
  public static void WriteCDataElement(this XmlWriter xmlWriter, String elementName, String cData)
  {
    xmlWriter.WriteStartElement(elementName);
    xmlWriter.WriteAttributeString("type", "text");
    xmlWriter.WriteCData(cData);
    xmlWriter.WriteEndElement();
  }

  /// <summary>
  /// Return the <paramref name="node"/>'s last child's inner text.
  /// </summary>
  /// <param name="node">An <see cref="XmlNode"/>.</param>
  /// <param name="xpath">An XPath expression to locate <paramref name="node"/>.</param>
  /// <returns>A <see cref="String"/>.</returns>
  public static String GetLastChildsInnerText(this XmlNode node, String xpath)
  {
    var singleNode = node.SelectSingleNode(xpath);
    if ((singleNode == null) || (singleNode.LastChild == null))
      return "";
    else
      return singleNode.LastChild.InnerText;
  }

  /// <summary>
  /// Return the <paramref name="node"/>'s inner text.
  /// </summary>
  /// <param name="node">An <see cref="XmlNode"/>.</param>
  /// <param name="xpath">An XPath expression to locate <paramref name="node"/>.</param>
  /// <returns>A <see cref="String"/>.</returns>
  public static String GetNodesInnerText(this XmlNode node, String xpath)
  {
    var singleNode = node.SelectSingleNode(xpath);
    return (singleNode == null) ? "" : singleNode.InnerText;
  }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class XmlCommentAttribute : Attribute
{
  private String _value = "";

  public XmlCommentAttribute(String value)
    : base()
  {
    this._value = value;
    this.ShouldIndent = true;
    this.IndentSize = 2;
  }

  public String Value
  {
    /* If the return value isn't surrounded with spaces,
       the XML comment comes out looking like this:

         <!--Xml comment-->

     */
    get { return $" {this._value} "; }
    set { this._value = value; }
  }

  public Boolean ShouldIndent { get; set; }
  public Int32 IndentSize { get; set; }
}

