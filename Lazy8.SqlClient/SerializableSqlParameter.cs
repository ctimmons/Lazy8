/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Xml;

using Microsoft.Data.SqlClient;

namespace Lazy8.SqlClient;

[Serializable]
public class SerializableSqlParameter
{
  /* Sample usage:

     var originalSqlParameter = new SqlParameter() { set properties as usual };
     var originalSerializableSqlParameter = new SerializableSqlParameter(originalSqlParameter);
     var serializedRepresentation = Serializer.Serialize(originalSerializableSqlParameter);
     ...
     var newSerializableSqlParameter = Serializer.Deserialize<SerializableSqlParameter>(serializedRepresentation);
     var newSqlParameter = newSerializableSqlParameter.GetSqlParameter(); */

  /* This class is a wrapper around the Microsoft.Data.SqlClient.SqlParameter type,
     and allows for the serialization/deserialization of that type.

     This class's sole purpose is for serialization/deserialization.  Code should not manipulate
     the public properties.

     SqlParameter does not have the Serializable attribute, nor does it implement the
     ISerializable or IXmlSerializable interfaces.  Therefore, all XML and JSON
     serialization mechanisms known to me are unable to handle SqlParameters.
     This includes all of the various XML and JSON classes in .Net, as well
     as NewtonSoft's JSON.Net library.

     One goal of serializing SqlParameter is that any serialization mechanism
     must not be tightly coupled to any specific serialization technology or format (xml, json, etc.).
     To accomplish this goal, this class has simple string properties corresponding to
     SqlParameter's public properties.  Even the most primitive serialization mechanism should be
     able to successfully handle string properties (but not Newtonsoft's JSON.Net - see "Note 1" below).
     Conversion between this class's string representations and the various SqlParameter
     property types are handled in the special SerializableSqlParameter(SqlParameter) constructor,
     and the GetSqlParameter() method.  See "Usage" above and the unit tests in Lazy8.Core.Tests.

     Setting SqlParameter properties has some strange side-effects.  Without drowning you
     in too many details as to why, the SqlValue and DbType properties are ignored for serialization
     purposes.  Setting the Value and SqlDbType properties are sufficient to set SqlParameter
     to a correct state.  See "Note 2" below for an excruciatingly detailed explanation as to
     how many side-effects are present in SqlParameter's property setters.

     ------

       Note 1:

       Newtonsoft's JSON.Net has a questionable "feature" regarding date/time values
       stored in strings.

       Under some conditions, upon seeing a string that looks like a date/time,
       JSON.Net will arbitrarily convert the string to a format it thinks is appropriate
       (no, I'm not making this up).

       For example, for JSON like this:

         {"PropertyName":"2022-01-30T01:43:23.5414138-06:00"}

       JSON.Net will (sometimes) deserialize the property's value to this:

         1/30/2022 1:43:23 AM

       Although this clearly violates the Principle of Least Astonishment[0],
       JSON.Net's author is adamant that this design is what he intended[1].

       [0] https://en.wikipedia.org/wiki/Principle_of_least_astonishment
       [1] https://github.com/JamesNK/Newtonsoft.Json/issues/862#issuecomment-237539075

       In order to avoid this surreal behavior without forcing the user to set
       JSON.Net's DateParseHandling property, the serialization and deserialization code
       in this class modifies any date/time strings it generates by prepending the string
       with a tilde (~) character for serialization, and stripping off that character for
       deserialization.  This seems to successfully prevent JSON.Net's arguably
       wrong default behavior.

     ------

       Note 2:

       Setting some of SqlParameter's properties has the side-effect of setting other
       property values.

       This somewhat resembles quantum entanglement - the ability of separated objects
       to share a condition or state, which Albert Einstein colorfully dismissed as
       "spooky action at a distance".  It seems Microsoft never got the memo, and thought
       implementing quantum entanglement in their code is a "Good Thing", thereby
       giving simple programmers like you and me endless headaches.

         (When referring to "Sql*" types and values, I mean the primitive type wrappers in the
         System.Data.SqlTypes namespace.  E.g. SqlString, SqlDecimal, etc.)

         - Setting the DbType property also sets the SqlDbType property to a corresponding value,
           and vice versa.
         - Some values of DbType are invalid when applied to SqlParameter, and will throw
           an exception.  The invalid DbType values are SByte, UInt16, UInt32, UInt64, and VarNumeric.
         - When either Value or SqlValue is set, both DbType and SqlDbType are also set
           (if possible - see below).
         - Setting Value to a Char or Char[] value will silently convert Value to a string.
         - When either Value or SqlValue is set to an Sql* value, they both refer to that value.
         - When either Value or SqlValue is set to a non-Sql* value that maps to an
           Sql* value, Value will contain the non-Sql* value, and SqlValue will contain the
           Sql* value.  For example, setting Value to an ordinary string causes SqlValue to be set
           to an SqlString.  Setting SqlValue to an integer like 42 causes Value to contain
           a boxed Int32 containing 42, and SqlValue will be set to an SqlInt32 containing 42.
         - When either Value or SqlValue is set to a value of a type that does not correspond
           to an Sql* type, neither property is set, nor is an exception thrown.  However, the
           SqlParameter instance is in an invalid state, and will throw an exception when
           it is processed by an SqlCommand.
         - When Value and SqlValue have valid data, but DbType and/or SqlDbType are set
           to values that conflict with what Value and SqlValue contain, the SqlParameter
           instance is in an invalid state, and will throw an exception when it is processed
           by an SqlCommand.  For example, setting Value to the string "foobar", and setting
           SqlDbType to BigInt.
         - It's documented that setting Value or SqlValue to null has different semantics
           than setting those properties to DbNull.Value. (See the "Remarks" section for
           either of those two properties for an explanation.)

           https://docs.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlparameter.sqlvalue
           https://docs.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlparameter.value

       Given all of this, the easiest path is to ignore DbType and SqlValue.  Both are set
       by other properties to correct values, and neither adds any real functionality,
       so there's no need for them to be involved in the serialization/deserialization process. */

  /* These properties are intended only for serialization/deserialization.
     Do not directly manipulate them. 

     The setters must be public for serialization to work. */

  public String CompareInfo { get; set; }
  public String Direction { get; set; }
  public String ForceColumnEncryption { get; set; }
  public String IsNullable { get; set; }
  public String LocaleId { get; set; }
  public String Offset { get; set; }
  public String ParameterName { get; set; }
  public String Precision { get; set; }
  public String Scale { get; set; }
  public String Size { get; set; }
  public String SourceColumn { get; set; } = "";
  public String SourceColumnNullMapping { get; set; }
  public String SourceVersion { get; set; }
  public String SqlDbType { get; set; }
  public String TypeName { get; set; }
  public String UdtTypeName { get; set; }
  public String Value { get; set; }
  public String ValueType { get; set; }
  public String XmlSchemaCollectionDatabase { get; set; }
  public String XmlSchemaCollectionName { get; set; }
  public String XmlSchemaCollectionOwningSchema { get; set; }

  /* Standard types that can be referenced by the Value property. */

  private const String _boolean = "Boolean";
  private const String _byte = "Byte";
  private const String _byteArray = "ByteArray";
  private const String _char = "Char";
  private const String _charArray = "CharArray";
  private const String _datetime = "DateTime";
  private const String _datetimeOffset = "DateTimeOffset";
  private const String _dbnull = "DBNull";
  private const String _decimal = "Decimal";
  private const String _double = "Double";
  private const String _guid = "Guid";
  private const String _int16 = "Int16";
  private const String _int32 = "Int32";
  private const String _int64 = "Int64";
  private const String _object = "Object";
  private const String _single = "Single";
  private const String _string = "String";
  private const String _timespan = "TimeSpan";

  /* Specialied Sql* types that can be referenced by the Value property. */

  private const String _sqlBinary = "SqlBinary";
  private const String _sqlBoolean = "SqlBoolean";
  private const String _sqlByte = "SqlByte";
  private const String _sqlBytes = "SqlBytes";
  private const String _sqlChars = "SqlChars";
  private const String _sqlDateTime = "SqlDateTime";
  private const String _sqlDecimal = "SqlDecimal";
  private const String _sqlDouble = "SqlDouble";
  private const String _sqlGuid = "SqlGuid";
  private const String _sqlInt16 = "SqlInt16";
  private const String _sqlInt32 = "SqlInt32";
  private const String _sqlInt64 = "SqlInt64";
  private const String _sqlMoney = "SqlMoney";
  private const String _sqlSingle = "SqlSingle";
  private const String _sqlString = "SqlString";
  private const String _sqlXml = "SqlXml";

  /* Special value written to ValueType when the Value property is null.

     Some serialization libraries, like NewtonSoft's JSON.Net,
     can behave strangely when deserializing a null property. 
     JSON.Net will insist the property's type is its proprietary
     JValue type. (This behavior is documented[0], but still strikes
     me as a bit odd).

     (See the section on "Untyped Objects")
     [0] https://www.newtonsoft.com/json/help/html/SerializationGuide.htm */

  private const String _null = "null";

  /* A parameterless constructor is required for serialization and deserialization.
     But please don't call this to construct an instance of this class. */

  public SerializableSqlParameter() : base() { }

  /* Use this constructor to get a serializable instance of this class. */

  public SerializableSqlParameter(SqlParameter sqlParameter) : this()
  {
    /* Note that SqlParameter's DbType and SqlValue properties are not serialized.
       See the comments at the beginning of this class as to why these two
       properties can and should be ignored. */

    /* SqlParameter's Value property has type Object, which means it can hold a reference to anything.
       However, only a small subset of possible references are legal for this property.
       For example, the following is not valid:

         var sqlParameter = new SqlParameter();
         sqlParameter.Value = new Exception();

       Obviously an instance of System.Exception cannot be sent to SQL Server.
       But - perhaps unexpectedly - no exception is thrown, even though the
       sqlParameter variable is in an invalid state.  An exception will be thrown
       when an attempt is made to send sqlParameter to the server.

       Remember that setting Value also sets SqlValue, DbType, and SqlDbType.
       The invalid state can be detected by trying to read DbType or SqlDbType.
       If the code that sets Value cannot determine an appropriate corresponding
       type value for DbType and SqlDbType, reading either one of those properties
       will throw an ArgumentException:

         var sqlParameter = new SqlParameter();
         sqlParameter.Value = new Exception();

         try
         {
           Console.WriteLine(sqlParameter.SqlDbType);
         }
         catch (ArgumentException ex)
         {
           Console.WriteLine($"'Value' contains an illegal value.\n\n{ex.Message}");
         }

       So the first thing this method does is check to see if sqlParameter is valid
       by reading its SqlDbType. */

    /* Reading sqlParameter.SqlDbType will throw an ArgumentException if sqlParameter
       is in an invalid state. */
    this.SqlDbType = sqlParameter.SqlDbType.ToString();

    if (sqlParameter.Value is null)
    {
      this.Value = "";
      this.ValueType = _null;
    }
    else if (sqlParameter.Value is Boolean boolean)
    {
      this.Value = boolean.ToString();
      this.ValueType = _boolean;
    }
    else if (sqlParameter.Value is Byte @byte)
    {
      this.Value = @byte.ToString();
      this.ValueType = _byte;
    }
    else if (sqlParameter.Value is Byte[] byteArray)
    {
      this.Value = Convert.ToBase64String(byteArray);
      this.ValueType = _byteArray;
    }
    else if (sqlParameter.Value is Char @char)
    {
      this.Value = @char.ToString();
      this.ValueType = _char;
    }
    else if (sqlParameter.Value is Char[] charArray)
    {
      this.Value = charArray.ToString();
      this.ValueType = _charArray;
    }
    else if (sqlParameter.Value is DateTime datetime)
    {
      /* Prepending a tilde to prevent Newtonsoft's JSON.Net from screwing things up during deserialization. */
      this.Value = "~" + datetime.ToString("O", CultureInfo.InvariantCulture);
      this.ValueType = _datetime;
    }
    else if (sqlParameter.Value is DateTimeOffset datetimeOffset)
    {
      /* Prepending a tilde to prevent Newtonsoft's JSON.Net from screwing things up during deserialization. */
      this.Value = "~" + datetimeOffset.ToString("O", CultureInfo.InvariantCulture);
      this.ValueType = _datetimeOffset;
    }
    else if (sqlParameter.Value is DBNull)
    {
      this.Value = _dbnull;
      this.ValueType = _dbnull;
    }
    else if (sqlParameter.Value is Decimal @decimal)
    {
      this.Value = @decimal.ToString();
      this.ValueType = _decimal;
    }
    else if (sqlParameter.Value is Double @double)
    {
      this.Value = @double.ToString();
      this.ValueType = _double;
    }
    else if (sqlParameter.Value is Guid guid)
    {
      this.Value = guid.ToString("N");
      this.ValueType = _guid;
    }
    else if (sqlParameter.Value is Int16 int16)
    {
      this.Value = int16.ToString();
      this.ValueType = _int16;
    }
    else if (sqlParameter.Value is Int32 int32)
    {
      this.Value = int32.ToString();
      this.ValueType = _int32;
    }
    else if (sqlParameter.Value is Int64 int64)
    {
      this.Value = int64.ToString();
      this.ValueType = _int64;
    }
    else if (sqlParameter.Value is Single single)
    {
      this.Value = single.ToString();
      this.ValueType = _single;
    }
    else if (sqlParameter.Value is String @string)
    {
      this.Value = @string;
      this.ValueType = _string;
    }
    else if (sqlParameter.Value is TimeSpan timespan)
    {
      /* Prepending a tilde to prevent Newtonsoft's JSON.Net from screwing things up during deserialization. */
      this.Value = "~" + timespan.ToString("c", CultureInfo.InvariantCulture);
      this.ValueType = _timespan;
    }
    else if (sqlParameter.Value is SqlBinary binary)
    {
      this.Value = Convert.ToBase64String(binary.Value);
      this.ValueType = _sqlBinary;
    }
    else if (sqlParameter.Value is SqlBoolean sqlBoolean)
    {
      this.Value = sqlBoolean.Value.ToString();
      this.ValueType = _sqlBoolean;
    }
    else if (sqlParameter.Value is SqlByte sqlByte)
    {
      this.Value = sqlByte.Value.ToString();
      this.ValueType = _sqlByte;
    }
    else if (sqlParameter.Value is SqlBytes sqlBytes)
    {
      this.Value = Convert.ToBase64String(sqlBytes.Value);
      this.ValueType = _sqlBytes;
    }
    else if (sqlParameter.Value is SqlChars sqlChars)
    {
      this.Value = new String(sqlChars.Value);
      this.ValueType = _sqlChars;
    }
    else if (sqlParameter.Value is SqlDateTime sqlDateTime)
    {
      /* Prepending a tilde to prevent Newtonsoft's JSON.Net from screwing things up during deserialization. */
      this.Value = "~" + sqlDateTime.Value.ToString("O", CultureInfo.InvariantCulture);
      this.ValueType = _sqlDateTime;
    }
    else if (sqlParameter.Value is SqlDecimal sqlDecimal)
    {
      this.Value = sqlDecimal.Value.ToString();
      this.ValueType = _sqlDecimal;
    }
    else if (sqlParameter.Value is SqlDouble sqlDouble)
    {
      this.Value = sqlDouble.Value.ToString();
      this.ValueType = _sqlDouble;
    }
    else if (sqlParameter.Value is SqlGuid sqlGuid)
    {
      this.Value = sqlGuid.Value.ToString("N");
      this.ValueType = _sqlGuid;
    }
    else if (sqlParameter.Value is SqlInt16 sqlInt16)
    {
      this.Value = sqlInt16.Value.ToString();
      this.ValueType = _sqlInt16;
    }
    else if (sqlParameter.Value is SqlInt32 sqlInt32)
    {
      this.Value = sqlInt32.Value.ToString();
      this.ValueType = _sqlInt32;
    }
    else if (sqlParameter.Value is SqlInt64 sqlInt64)
    {
      this.Value = sqlInt64.Value.ToString();
      this.ValueType = _sqlInt64;
    }
    else if (sqlParameter.Value is SqlMoney sqlMoney)
    {
      this.Value = sqlMoney.Value.ToString();
      this.ValueType = _sqlMoney;
    }
    else if (sqlParameter.Value is SqlSingle sqlSingle)
    {
      this.Value = sqlSingle.Value.ToString();
      this.ValueType = _sqlSingle;
    }
    else if (sqlParameter.Value is SqlString sqlString)
    {
      this.Value = sqlString.Value;
      this.ValueType = _sqlString;
    }
    else if (sqlParameter.Value is SqlXml sqlXml)
    {
      this.Value = sqlXml.Value;
      this.ValueType = _sqlXml;
    }
    else
    {
      this.Value = sqlParameter.Value.ToString();
      this.ValueType = _object;
    }

    this.CompareInfo = sqlParameter.CompareInfo.ToString();
    this.Direction = sqlParameter.Direction.ToString();
    this.ForceColumnEncryption = sqlParameter.ForceColumnEncryption.ToString();
    this.IsNullable = sqlParameter.IsNullable.ToString();
    this.LocaleId = sqlParameter.LocaleId.ToString();
    this.Offset = sqlParameter.Offset.ToString();
    this.ParameterName = sqlParameter.ParameterName;
    this.Precision = sqlParameter.Precision.ToString();
    this.Scale = sqlParameter.Scale.ToString();
    this.Size = sqlParameter.Size.ToString();
    this.SourceColumn = sqlParameter.SourceColumn;
    this.SourceColumnNullMapping = sqlParameter.SourceColumnNullMapping.ToString();
    this.SourceVersion = sqlParameter.SourceVersion.ToString();
    this.TypeName = sqlParameter.TypeName;
    this.UdtTypeName = sqlParameter.UdtTypeName;
    this.XmlSchemaCollectionDatabase = sqlParameter.XmlSchemaCollectionDatabase;
    this.XmlSchemaCollectionName = sqlParameter.XmlSchemaCollectionName;
    this.XmlSchemaCollectionOwningSchema = sqlParameter.XmlSchemaCollectionOwningSchema;
  }

  public SqlParameter GetSqlParameter()
  {
    /* After deserializing a SerializableSqlParameter, call this
       method to get the underlying SqlParameter. */

    SqlParameter result = new()
    {
      /* Note that SqlParameter's DbType and SqlValue properties are not serialized.
         See the comments at the beginning of this class as to why these two
         properties can and should be ignored. */

      CompareInfo = (SqlCompareOptions) Enum.Parse(typeof(SqlCompareOptions), this.CompareInfo, ignoreCase: true),
      Direction = (ParameterDirection) Enum.Parse(typeof(ParameterDirection), this.Direction, ignoreCase: true),
      ForceColumnEncryption = Boolean.Parse(this.ForceColumnEncryption),
      IsNullable = Boolean.Parse(this.IsNullable),
      LocaleId = Convert.ToInt32(this.LocaleId),
      Offset = Convert.ToInt32(this.Offset),
      ParameterName = this.ParameterName,
      Precision = Convert.ToByte(this.Precision),
      Scale = Convert.ToByte(this.Scale),
      Size = Convert.ToInt32(this.Size),
      SourceColumn = this.SourceColumn,
      SourceColumnNullMapping = Boolean.Parse(this.SourceColumnNullMapping),
      SourceVersion = (DataRowVersion) Enum.Parse(typeof(DataRowVersion), this.SourceVersion, ignoreCase: true),
      TypeName = this.TypeName,
      UdtTypeName = this.UdtTypeName,
      XmlSchemaCollectionDatabase = this.XmlSchemaCollectionDatabase,
      XmlSchemaCollectionName = this.XmlSchemaCollectionName,
      XmlSchemaCollectionOwningSchema = this.XmlSchemaCollectionOwningSchema
    };

    switch (this.ValueType)
    {
      case _null:
        result.Value = null;
        break;
      case _boolean:
        result.Value = Boolean.Parse(this.Value);
        break;
      case _byte:
        result.Value = Convert.ToByte(this.Value);
        break;
      case _byteArray:
        result.Value = Convert.FromBase64String(this.Value);
        break;
      case _char:
        result.Value = Convert.ToChar(this.Value);
        break;
      case _charArray:
        result.Value = this.Value.ToCharArray();
        break;
      case _datetime:
        /* Strip off the leading tilde added during serialization. The tilde
           was added to prevent Newtonsoft's JSON.Net from screwing things up. */
        result.Value = DateTime.ParseExact(this.Value[1..], "O", CultureInfo.InvariantCulture);
        break;
      case _datetimeOffset:
        /* Strip off the leading tilde added during serialization. The tilde
           was added to prevent Newtonsoft's JSON.Net from screwing things up. */
        result.Value = DateTimeOffset.ParseExact(this.Value[1..], "O", CultureInfo.InvariantCulture);
        break;
      case _dbnull:
        result.Value = DBNull.Value;
        break;
      case _decimal:
        result.Value = Convert.ToDecimal(this.Value);
        break;
      case _double:
        result.Value = Convert.ToDouble(this.Value);
        break;
      case _guid:
        result.Value = Guid.ParseExact(this.Value, "N");
        break;
      case _int16:
        result.Value = Convert.ToInt16(this.Value);
        break;
      case _int32:
        result.Value = Convert.ToInt32(this.Value);
        break;
      case _int64:
        result.Value = Convert.ToInt64(this.Value);
        break;
      case _single:
        result.Value = Convert.ToSingle(this.Value);
        break;
      case _string:
        result.Value = this.Value;
        break;
      case _timespan:
        /* Strip off the leading tilde added during serialization. The tilde
           was added to prevent Newtonsoft's JSON.Net from screwing things up. */
        result.Value = TimeSpan.ParseExact(this.Value[1..], "c", CultureInfo.InvariantCulture);
        break;
      case _sqlBinary:
        result.Value = new SqlBinary(Convert.FromBase64String(this.Value));
        break;
      case _sqlBoolean:
        result.Value = new SqlBoolean(Boolean.Parse(this.Value));
        break;
      case _sqlByte:
        result.Value = new SqlByte(Convert.ToByte(this.Value));
        break;
      case _sqlBytes:
        result.Value = new SqlBytes(Convert.FromBase64String(this.Value));
        break;
      case _sqlChars:
        result.Value = new SqlChars(this.Value.ToCharArray());
        break;
      case _sqlDateTime:
        /* Strip off the leading tilde added during serialization. The tilde
           was added to prevent Newtonsoft's JSON.Net from screwing things up. */
        result.Value = new SqlDateTime(DateTime.ParseExact(this.Value[1..], "O", CultureInfo.InvariantCulture));
        break;
      case _sqlDecimal:
        result.Value = new SqlDecimal(Convert.ToDecimal(this.Value));
        break;
      case _sqlDouble:
        result.Value = new SqlDouble(Convert.ToDouble(this.Value));
        break;
      case _sqlGuid:
        result.Value = new SqlGuid(this.Value);
        break;
      case _sqlInt16:
        result.Value = new SqlInt16(Convert.ToInt16(this.Value));
        break;
      case _sqlInt32:
        result.Value = new SqlInt32(Convert.ToInt32(this.Value));
        break;
      case _sqlInt64:
        result.Value = new SqlInt64(Convert.ToInt64(this.Value));
        break;
      case _sqlMoney:
        result.Value = new SqlMoney(Convert.ToDecimal(this.Value));
        break;
      case _sqlSingle:
        result.Value = new SqlSingle(Convert.ToSingle(this.Value));
        break;
      case _sqlString:
        result.Value = new SqlString(this.Value);
        break;
      case _sqlXml:
        using (var stringReader = new StringReader(this.Value))
          using (var xmlTextReader = new XmlTextReader(stringReader))
            result.Value = new SqlXml(xmlTextReader);
        break;
      case _object:
        result.Value = this.Value;
        break;
      default:
        throw new InvalidOperationException($"Unknown {nameof(this.ValueType)} ('{this.Value}').");
    }

    /* Setting Value has the side-effect of also setting SqlDbType to a calculated "good enough" value.
       It's possible the owner of the deserialized SqlParameter instance chose a different value for
       SqlDbType.  Let the owner's choice override the calculated SqlDbType value by assigning
       SqlDbType *after* setting Value. */

    result.SqlDbType = (SqlDbType) Enum.Parse(typeof(SqlDbType), this.SqlDbType, ignoreCase: true);

    return result;
  }
}

