using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Text.Json;
using System.Xml;

using Microsoft.Data.SqlClient;

using Newtonsoft.Json;

using NUnit.Framework;

using Lazy8.Core;

namespace Lazy8.SqlClient.Tests
{
  [TestFixture]
  public class SerializableSqlParameterTests
  {
    [Test]
    public void RoundTripPropertiesOtherThanValueTest()
    {
      var expectedSqlParameter =
        new SqlParameter()
        {
          CompareInfo = SqlCompareOptions.IgnoreKanaType,
          Direction = ParameterDirection.Output,
          ForceColumnEncryption = true,
          IsNullable = true,
          LocaleId = 42,
          Offset = 69,
          ParameterName = "ParameterName",
          Precision = 99,
          Scale = 138,
          Size = 30,
          SourceColumn = "SourceColumn",
          SourceColumnNullMapping = true,
          SourceVersion = DataRowVersion.Proposed,
          TypeName = "TypeName",
          UdtTypeName = "UdtTypeName",
          XmlSchemaCollectionDatabase = "XmlSchemaCollectionDatabase",
          XmlSchemaCollectionName = "XmlSchemaCollectionName",
          XmlSchemaCollectionOwningSchema = "XmlSchemaCollectionOwningSchema"
        };

      this.RoundTripSqlParameter(expectedSqlParameter, this.CompareRoundTripPropertiesOtherThanValue);
    }

    private void RoundTripSqlParameter(SqlParameter expectedSqlParameter, Action<SqlParameter, SqlParameter> comparisonPredicate)
    {
      var expectedSerializedSqlParameter = new SerializableSqlParameter(expectedSqlParameter);

      /* Do roundtrips thru all three of the most commonly used serialization technologies:
         Newtonsoft JSON.Net, .Net's System.Text.Json, and .Net's XML classes. */

      /* Newtonsoft JSON.Net */

      var settings = new JsonSerializerSettings
      {
        Formatting = Newtonsoft.Json.Formatting.Indented
      };

      var json = JsonConvert.SerializeObject(expectedSerializedSqlParameter, settings);
      var actualSerializedSqlParameter = JsonConvert.DeserializeObject<SerializableSqlParameter>(json, settings);
      var actualSqlParameter = actualSerializedSqlParameter.GetSqlParameter();
      comparisonPredicate(expectedSqlParameter, actualSqlParameter);

      /* System.Text.Json */

      JsonSerializerOptions options = new() { WriteIndented = true };

      json = System.Text.Json.JsonSerializer.Serialize(expectedSerializedSqlParameter, options);
      actualSerializedSqlParameter = System.Text.Json.JsonSerializer.Deserialize<SerializableSqlParameter>(json, options);
      actualSqlParameter = actualSerializedSqlParameter.GetSqlParameter();
      comparisonPredicate(expectedSqlParameter, actualSqlParameter);

      /* XML */

      json = XmlUtils.SerializeObjectToXmlString(expectedSerializedSqlParameter);
      actualSerializedSqlParameter = XmlUtils.DeserializeObjectFromXmlString<SerializableSqlParameter>(json);
      actualSqlParameter = actualSerializedSqlParameter.GetSqlParameter();
      comparisonPredicate(expectedSqlParameter, actualSqlParameter);
    }

    private void CompareRoundTripPropertiesOtherThanValue(SqlParameter expectedSqlParameter, SqlParameter actualSqlParameter)
    {
      Assert.Multiple(
        () =>
        {
          Assert.That(expectedSqlParameter.CompareInfo, Is.EqualTo(actualSqlParameter.CompareInfo), "CompareInfo");
          Assert.That(expectedSqlParameter.Direction, Is.EqualTo(actualSqlParameter.Direction), "Direction");
          Assert.That(expectedSqlParameter.ForceColumnEncryption, Is.EqualTo(actualSqlParameter.ForceColumnEncryption), "ForceColumnEncryption");
          Assert.That(expectedSqlParameter.IsNullable, Is.EqualTo(actualSqlParameter.IsNullable), "IsNullable");
          Assert.That(expectedSqlParameter.LocaleId, Is.EqualTo(actualSqlParameter.LocaleId), "LocaleId");
          Assert.That(expectedSqlParameter.Offset, Is.EqualTo(actualSqlParameter.Offset), "Offset");
          Assert.That(expectedSqlParameter.ParameterName, Is.EqualTo(actualSqlParameter.ParameterName), "ParameterName");
          Assert.That(expectedSqlParameter.Precision, Is.EqualTo(actualSqlParameter.Precision), "Precision");
          Assert.That(expectedSqlParameter.Scale, Is.EqualTo(actualSqlParameter.Scale), "Scale");
          Assert.That(expectedSqlParameter.Size, Is.EqualTo(actualSqlParameter.Size), "Size");
          Assert.That(expectedSqlParameter.SourceColumn, Is.EqualTo(actualSqlParameter.SourceColumn), "SourceColumn");
          Assert.That(expectedSqlParameter.SourceColumnNullMapping, Is.EqualTo(actualSqlParameter.SourceColumnNullMapping), "SourceColumnNullMapping");
          Assert.That(expectedSqlParameter.SourceVersion, Is.EqualTo(actualSqlParameter.SourceVersion), "SourceVersion");
          Assert.That(expectedSqlParameter.TypeName, Is.EqualTo(actualSqlParameter.TypeName), "TypeName");
          Assert.That(expectedSqlParameter.UdtTypeName, Is.EqualTo(actualSqlParameter.UdtTypeName), "UdtTypeName");
          Assert.That(expectedSqlParameter.XmlSchemaCollectionDatabase, Is.EqualTo(actualSqlParameter.XmlSchemaCollectionDatabase), "XmlSchemaCollectionDatabase");
          Assert.That(expectedSqlParameter.XmlSchemaCollectionName, Is.EqualTo(actualSqlParameter.XmlSchemaCollectionName), "XmlSchemaCollectionName");
          Assert.That(expectedSqlParameter.XmlSchemaCollectionOwningSchema, Is.EqualTo(actualSqlParameter.XmlSchemaCollectionOwningSchema), "XmlSchemaCollectionOwningSchema");
        });
    }

    private void CompareRoundTripValueProperty(SqlParameter expectedSqlParameter, SqlParameter actualSqlParameter)
    {
      if ((expectedSqlParameter.Value is Boolean expectedBoolean) && (actualSqlParameter.Value is Boolean actualBoolean))
        Assert.That(expectedBoolean, Is.EqualTo(actualBoolean), "(Boolean) Value");
      else if ((expectedSqlParameter.Value is Byte expectedByte) && (actualSqlParameter.Value is Byte actualByte))
        Assert.That(expectedByte, Is.EqualTo(actualByte), "(Byte) Value");
      else if ((expectedSqlParameter.Value is Byte[] expectedByteArray) && (actualSqlParameter.Value is Byte[] actualByteArray))
        Assert.That(Convert.ToBase64String(expectedByteArray), Is.EqualTo(Convert.ToBase64String(actualByteArray)), "(Byte[]) Value");
      else if ((expectedSqlParameter.Value is Char expectedChar) && (actualSqlParameter.Value is Char actualChar))
        Assert.That(expectedChar, Is.EqualTo(actualChar), "(Char) Value");
      else if ((expectedSqlParameter.Value is Char[] expectedCharArray) && (actualSqlParameter.Value is Char[] actualCharArray))
        Assert.That(expectedCharArray, Is.EqualTo(actualCharArray), "(Char[]) Value");
      else if ((expectedSqlParameter.Value is DateTime expectedDatetime) && (actualSqlParameter.Value is DateTime actualDatetime))
        Assert.That(expectedDatetime, Is.EqualTo(actualDatetime), "(DateTime) Value");
      else if ((expectedSqlParameter.Value is DateTimeOffset expectedDatetimeOffset) && (actualSqlParameter.Value is DateTimeOffset actualDatetimeOffset))
        Assert.That(expectedDatetimeOffset, Is.EqualTo(actualDatetimeOffset), "(DateTimeOffset) Value");
      else if ((expectedSqlParameter.Value is DBNull) && (actualSqlParameter.Value is DBNull))
        /* If both the expected and actual Value properties are DBNull, they are by definition equal. */
        return;
      else if ((expectedSqlParameter.Value is Decimal expectedDecimal) && (actualSqlParameter.Value is Decimal actualDecimal))
        Assert.That(expectedDecimal, Is.EqualTo(actualDecimal), "(Decimal) Value");
      else if ((expectedSqlParameter.Value is Double expectedDouble) && (actualSqlParameter.Value is Double actualDouble))
        Assert.That(expectedDouble, Is.EqualTo(actualDouble), "(Double) Value");
      else if ((expectedSqlParameter.Value is Guid expectedGuid) && (actualSqlParameter.Value is Guid actualGuid))
        Assert.That(expectedGuid, Is.EqualTo(actualGuid), "(Guid) Value");
      else if ((expectedSqlParameter.Value is Int16 expectedInt16) && (actualSqlParameter.Value is Int16 actualInt16))
        Assert.That(expectedInt16, Is.EqualTo(actualInt16), "(Int16) Value");
      else if ((expectedSqlParameter.Value is Int32 expectedInt32) && (actualSqlParameter.Value is Int32 actualInt32))
        Assert.That(expectedInt32, Is.EqualTo(actualInt32), "(Int32) Value");
      else if ((expectedSqlParameter.Value is Int64 expectedInt64) && (actualSqlParameter.Value is Int64 actualInt64))
        Assert.That(expectedInt64, Is.EqualTo(actualInt64), "(Int64) Value");
      else if ((expectedSqlParameter.Value is Single expectedSingle) && (actualSqlParameter.Value is Single actualSingle))
        Assert.That(expectedSingle, Is.EqualTo(actualSingle), "(Single) Value");
      else if ((expectedSqlParameter.Value is String expectedString) && (actualSqlParameter.Value is String actualString))
        Assert.That(expectedString, Is.EqualTo(actualString), "(String) Value");
      else if ((expectedSqlParameter.Value is TimeSpan expectedTimespan) && (actualSqlParameter.Value is TimeSpan actualTimespan))
        Assert.That(expectedTimespan, Is.EqualTo(actualTimespan), "(TimeSpan) Value");
      else if ((expectedSqlParameter.Value is SqlBytes expectedSqlBytes) && (actualSqlParameter.Value is SqlBytes actualSqlBytes))
        Assert.That(Convert.ToBase64String(expectedSqlBytes.Value), Is.EqualTo(Convert.ToBase64String(actualSqlBytes.Value)), "(SqlBytes) Value");
      else if ((expectedSqlParameter.Value is SqlChars expectedSqlChars) && (actualSqlParameter.Value is SqlChars actualSqlChars))
        Assert.That(expectedSqlChars.Value.ToString(), Is.EqualTo(actualSqlChars.Value.ToString()), "(SqlChars) Value");
      else if ((expectedSqlParameter.Value is SqlXml expectedSqlXml) && (actualSqlParameter.Value is SqlXml actualSqlXml))
        Assert.That(expectedSqlXml.Value, Is.EqualTo(actualSqlXml.Value), "(SqlXml) Value");
      else
        /* For all other types, use the type's Equal method.  Most of the Sql* types override this method. */
        Assert.That(expectedSqlParameter.Value, Is.EqualTo(actualSqlParameter.Value), "Default Equal() method.");
    }

    [Test]
    public void RoundTripValuePropertyTest()
    {
      var byteArray = new Byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

      var xml = "<root><el1>Something</el1></root>";
      var xmlDocument = new XmlDocument();
      xmlDocument.LoadXml(xml);

      var valueTestCases =
        new List<(Object value, Func<Object, Object, Boolean> predicate)>()
        {
          (true, Equals),
          ((Byte) 42, Equals),
          (byteArray, (p1, p2) => ((Byte[]) p1).SequenceEqual((Byte[]) p2)),

          /* SqlParameter.Value coerces a character to a string. */
          ('X', Equals),

          /* SqlParameter.Value coerces a character array to a string. */
          ("foo".ToCharArray(), Equals),

          (DateTime.Now, Equals),
          (DateTimeOffset.Now, Equals),
          (DBNull.Value, (p1, p2) => Convert.IsDBNull(p1) && Convert.IsDBNull(p2)),
          ((Decimal) 42.69, Equals),
          ((Double) 42.69, Equals),
          (Guid.NewGuid(), (p1, p2) => ((Guid) p1).Equals((Guid) p2)),
          ((Int16) 42, Equals),
          ((Int32) 42, Equals),
          ((Int64) 42, Equals),
          ((Single) 42.69, Equals),
          ("foo", Equals),
          (TimeSpan.FromDays(69), Equals),
          (new SqlBinary(byteArray), (p1, p2) => ((SqlBinary) p1).Value.SequenceEqual(((SqlBinary) p2).Value)),
          (new SqlBoolean(true), (p1, p2) => ((SqlBoolean) p1).Value == ((SqlBoolean) p2).Value),
          (new SqlByte(42), (p1, p2) => ((SqlByte) p1).Value == ((SqlByte) p2).Value),
          (new SqlBytes(byteArray), (p1, p2) => ((SqlBytes) p1).Value.SequenceEqual(((SqlBytes) p2).Value)),
          (new SqlChars("foo".ToCharArray()), (p1, p2) => ((SqlChars) p1).Value.SequenceEqual(((SqlChars) p2).Value)),
          (new SqlDateTime(DateTime.Now), (p1, p2) => ((SqlDateTime) p1).Value == ((SqlDateTime) p2).Value),
          (new SqlDecimal(42), (p1, p2) => ((SqlDecimal) p1).Value == ((SqlDecimal) p2).Value),
          (new SqlDouble(42.69), (p1, p2) => ((SqlDouble) p1).Value == ((SqlDouble) p2).Value),
          (new SqlGuid(Guid.NewGuid()), (p1, p2) => ((SqlGuid) p1).Value.Equals(((SqlGuid) p2).Value)),
          (new SqlInt16(42), (p1, p2) => ((SqlInt16) p1).Value == ((SqlInt16) p2).Value),
          (new SqlInt32(42), (p1, p2) => ((SqlInt32) p1).Value == ((SqlInt32) p2).Value),
          (new SqlInt64(42), (p1, p2) => ((SqlInt64) p1).Value == ((SqlInt64) p2).Value),
          (new SqlMoney(42.69), (p1, p2) => ((SqlMoney) p1).Value == ((SqlMoney) p2).Value),
          (new SqlSingle(42.69), (p1, p2) => ((SqlSingle) p1).Value == ((SqlSingle) p2).Value),
          (new SqlString("foo"), (p1, p2) => ((SqlString) p1).Value == ((SqlString) p2).Value),
          (new SqlXml(new XmlTextReader(xml, XmlNodeType.Document, null)), (p1, p2) => ((SqlXml) p1).Value == ((SqlXml) p2).Value)
        };

      foreach (var (value, predicate) in valueTestCases)
      {
        var expectedSqlParameter = new SqlParameter() { Value = value };
        this.RoundTripSqlParameter(expectedSqlParameter, this.CompareRoundTripValueProperty);
      }
    }
  }
}
