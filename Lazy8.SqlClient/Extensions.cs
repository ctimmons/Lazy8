/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using Lazy8.Core;

using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace Lazy8.SqlClient;

public static class SqlServerExtensionMethods
{
  /// <summary>
  /// Throws an exception if the connectionString parameter is either malformed or contains bad content.
  /// </summary>
  /// <param name="connectionString">A String which may or may not be a valid connection string.</param>
  public static void CheckConnectionString(this String connectionString)
  {
    /* Check connectionString's form.  This line of code throws an
       exception if connectionString is malformed. */
    _ = new SqlConnectionStringBuilder(connectionString);

    /* Check connectionString's content.  This code throws an exception
       if the server rejects connectionString. */
    using (var connection = new SqlConnection(connectionString))
      connection.Open();
  }

  /// <summary>
  /// Given an SqlConnection instance, return a clone of that connection.
  /// </summary>
  /// <param name="originalConnection">An SqlConnection instance.</param>
  /// <returns>An SqlConnection instance.</returns>
  public static SqlConnection GetClone(this SqlConnection originalConnection)
  {
    /* Why clone the connection?  Why not just create a new connection like this:
    
         return new SqlConnection(originalConnection.ConnectionString);
    
       Because using this approach won't copy any of originalConnection's modified properties,
       whereas cloning will (with exceptions noted below). */

    var clonedConnection = (SqlConnection) ((ICloneable) originalConnection).Clone();

    /* clonedConnection will always start out with a State of 'Closed', regardless of what
       originalConnection's State is.  Ensure clonedConnection's State property correctly
       reflects originalConnection's State property. */

    if ((originalConnection.State == ConnectionState.Connecting) || (originalConnection.State == ConnectionState.Open))
      clonedConnection.Open();

    /* BUG WORKAROUND

       The current (March 2024) help entries for both the .NetFx and .Net Core libraries for SqlConnection.Clone() state:

         "This member is only supported by the .NET Compact Framework."

         https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection.system-icloneable-clone

         and

         https://docs.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlconnection.system-icloneable-clone

       Two things are notable - 1) the compact framework hasn't been updated since 2013, and is apparently
       abandonware, and 2) aside from that single line in the help files, the Microsoft.Data.SqlClient project
       documentation and source code make no mention of the compact framework.

       This leads to the probable conclusion that this line in the help file is incorrect, and should be removed.

       Until that issue is clarified, we'll charge ahead with using the Clone() method on the latest .Net release,
       assuming the help file is wrong and that such calls are OK and won't blow up in our face.

       That being said, the Clone() method has at least one bug.

       Cloning an SqlConnection will NOT clone the original connection's Database property.
       The connection string's database is cloned instead.  These can differ if ChangeDatabase() was called
       on the original connection.  If necessary, the cloned connection must explicitly change
       the database to match the original connection's database. */

    if (clonedConnection.Database != originalConnection.Database)
      /* Don't check to see if clonedConnection is open.  Just let ChangeDatabase()
         throw an InvalidOperationException if clonedConnection isn't open.

         (clonedConnection's State *should* be open (see the previous line of code), but ADO.Net's implementation
         has so many corner cases it's possible that clonedConnection is closed at this point). */
      clonedConnection.ChangeDatabase(originalConnection.Database);

    return clonedConnection;
  }

  /// <summary>
  /// Executes the given sql on the connection, and
  /// returns the result wrapped in a <see cref="System.Data.DataSet">DataSet</see>.
  /// </summary>
  /// <param name="connection"><see cref="Microsoft.Data.SqlClient.SqlConnection">SqlConnection</see> the sql is sent to.  The connection must be opened before calling this method.</param>
  /// <param name="sql"><see cref="System.String">String</see> containing sql to execute.</param>
  /// <returns>A DataSet.</returns>
  public static DataSet GetDataSet(this SqlConnection connection, String sql, Int32 commandTimeout = 0)
  {
    using (var command = new SqlCommand() { Connection = connection, CommandTimeout = commandTimeout, CommandType = CommandType.Text, CommandText = sql })
      return GetDataSet(command);
  }

  /// <summary>
  /// Given an SqlConnection, execute the stored procedure in storeProcedureName on that connection,
  /// passing the optional sqlParameters to the stored procedure.  Return all of the result sets returned
  /// by the stored procedure in a DataSet.
  /// </summary>
  /// <param name="connection"><see cref="Microsoft.Data.SqlClient.SqlConnection">SqlConnection</see> the sql is sent to.  The connection must be opened before calling this method.</param>
  /// <param name="storedProcedureName">A valid stored procedure name.</param>
  /// <param name="parameters">An optional array of SqlParameters.  Defaults to null.</param>
  /// <returns>A DataSet.</returns>
  public static DataSet GetDataSetFromStoredProcedure(this SqlConnection connection, String storedProcedureName, params SqlParameter[] parameters)
  {
    storedProcedureName.Name(nameof(storedProcedureName)).NotNullEmptyOrOnlyWhitespace();

    using (var command = new SqlCommand() { Connection = connection, CommandType = CommandType.StoredProcedure, CommandText = storedProcedureName })
    {
      if (parameters is not null)
        command.Parameters.AddRange(parameters);

      return GetDataSet(command);
    }
  }

  /// <summary>
  /// Given an SqlCommand, return the results of the command in a DataSet.
  /// </summary>
  /// <param name="command">A valid SqlCommand instance.</param>
  /// <returns>A DataSet.</returns>
  public static DataSet GetDataSet(SqlCommand command)
  {
    command.Name(nameof(command)).NotNull();

    using (var adapter = new SqlDataAdapter())
    {
      var dataSet = new DataSet();
      adapter.SelectCommand = command;
      adapter.Fill(dataSet);
      return dataSet;
    }
  }

  /// <summary>
  /// Given an SqlConnection, execute the T-SQL in the sql parameter on that connection, and return
  /// the results as an IEnumerable&lt;DataRow&gt;.
  /// </summary>
  /// <param name="connection">A valid SqlConnection.</param>
  /// <param name="sql">A string containing T-SQL code.</param>
  /// <returns>An IEnumerable&lt;DataRow&gt;.</returns>
  public static IEnumerable<DataRow> GetDataRows(this SqlConnection connection, String sql) =>
    connection.GetDataSet(sql).Tables[0].Rows.Cast<DataRow>();

  /// <summary>
  /// Given an SqlConnection, execute the T-SQL in the sql parameter on that connection, and return
  /// the result in a single variable of type T.
  /// </summary>
  /// <typeparam name="T">The type of the returned value.</typeparam>
  /// <param name="connection">A valid SqlConnection.</param>
  /// <param name="sql">A string containing T-SQL code.</param>
  /// <returns>A value of type T.</returns>
  public static T GetSingleValue<T>(this SqlConnection connection, String sql) =>
    (T) connection.GetDataRows(sql).First()[0];

  /// <summary>
  /// Given an SqlConnection, execute the T-SQL in the sql parameter on that connection.
  /// </summary>
  /// <param name="connection">A valid SqlConnection.</param>
  /// <param name="sql">A string containing T-SQL code.</param>
  /// <returns>An Int32 indicating the number of rows affected.</returns>
  public static Int32 ExecuteNonQuery(this SqlConnection connection, String sql, Int32 commandTimeout = 0)
  {
    using (var command = new SqlCommand() { Connection = connection, CommandTimeout = commandTimeout, CommandType = CommandType.Text, CommandText = sql })
      return command.ExecuteNonQuery();
  }

  /// <summary>
  /// Given an SqlConnection, asynchronously execute the T-SQL in the sql parameter on that connection.
  /// </summary>
  /// <param name="connection">A valid SqlConnection.</param>
  /// <param name="sql">A string containing T-SQL code.</param>
  /// <returns>A Task&lt;Int32&gt; indicating the number of rows affected.</returns>
  public static async Task<Int32> ExecuteNonQueryAsync(this SqlConnection connection, String sql, Int32 commandTimeout = 0)
  {
    using (var command = new SqlCommand() { Connection = connection, CommandTimeout = commandTimeout, CommandType = CommandType.Text, CommandText = sql })
      return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// Execute the given action within a transaction on connection.  The transaction is committed if action does not
  /// throw an exception.  Likewise, if action throws any exception, the transaction is rolled back and the exception
  /// is re-thrown.
  /// </summary>
  /// <param name="connection">An SqlConnection.  The connection is assumed to be open.</param>
  /// <param name="action">An action that executes code on connection.</param>
  public static void ExecuteInTransaction(this SqlConnection connection, Action<SqlTransaction> action)
  {
    using (var transaction = connection.BeginTransaction())
    {
      try
      {
        action(transaction);
        transaction.Commit();
      }
      catch
      {
        transaction.Rollback();
        throw;
      }
    }
  }

  /// <summary>
  /// Given an <see cref="Microsoft.Data.SqlClient.SqlDataReader">SqlDataReader</see>,
  /// and a columnName, retrieve an XDocument from that column.
  /// </summary>
  /// <param name="sqlDataReader">A valid <see cref="Microsoft.Data.SqlClient.SqlDataReader">SqlDataReader</see>.</param>
  /// <param name="columnName">A String referring to a column name in the current row being read by sqlDataReader.</param>
  /// <exception cref="System.IndexOutOfRangeException">Thrown when columnIndex is out of range.</exception>
  /// <returns>An XDocument.</returns>
  public static XDocument GetXDocument(this SqlDataReader sqlDataReader, String columnName) =>
    sqlDataReader.GetXDocument(sqlDataReader.GetOrdinal(columnName));

  /// <summary>
  /// Given an <see cref="Microsoft.Data.SqlClient.SqlDataReader">SqlDataReader</see>,
  /// and a columnIndex, retrieve an XDocument from that column.
  /// </summary>
  /// <param name="sqlDataReader">A valid <see cref="Microsoft.Data.SqlClient.SqlDataReader">SqlDataReader</see>.</param>
  /// <param name="columnIndex">An Int32 referring to a column in the current row being read by sqlDataReader.</param>
  /// <exception cref="System.IndexOutOfRangeException">Thrown when columnIndex is out of range.</exception>
  /// <returns>An XDocument.</returns>
  public static XDocument GetXDocument(this SqlDataReader sqlDataReader, Int32 columnIndex)
  {
    using (var xmlReader = sqlDataReader.GetSqlXml(columnIndex).CreateReader())
      return XDocument.Load(xmlReader);
  }

  /// <summary>
  /// Given an <see cref="Microsoft.Data.SqlClient.SqlDataReader">SqlDataReader</see>,
  /// and a columnName, retrieve an XElement from that column.
  /// </summary>
  /// <param name="sqlDataReader">A valid <see cref="Microsoft.Data.SqlClient.SqlDataReader">SqlDataReader</see>.</param>
  /// <param name="columnName">A valid String name of one of sqlDataReader's columns.</param>
  /// <returns>An XElement.</returns>
  public static XElement GetXElement(this SqlDataReader sqlDataReader, String columnName) =>
    sqlDataReader.GetXElement(sqlDataReader.GetOrdinal(columnName));

  /// <summary>
  /// Given an <see cref="Microsoft.Data.SqlClient.SqlDataReader">SqlDataReader</see>,
  /// and a columnIndex, retrieve an XElement from that column.
  /// </summary>
  /// <param name="sqlDataReader">A valid <see cref="Microsoft.Data.SqlClient.SqlDataReader">SqlDataReader</see>.</param>
  /// <param name="columnIndex">A valid Int32 index of one of sqlDataReader's columns.</param>
  /// <exception cref="System.IndexOutOfRangeException">Thrown when columnIndex is out of range.</exception>
  /// <returns>An XElement.</returns>
  public static XElement GetXElement(this SqlDataReader sqlDataReader, Int32 columnIndex)
  {
    using (var xmlReader = sqlDataReader.GetSqlXml(columnIndex).CreateReader())
      return XElement.Load(xmlReader);
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="sqlDataReader"></param>
  /// <param name="columnName"></param>
  /// <returns></returns>
  public static XmlDocument GetXmlDocument(this SqlDataReader sqlDataReader, String columnName) =>
    sqlDataReader.GetXmlDocument(sqlDataReader.GetOrdinal(columnName));

  /// <summary>
  /// Retrieving an <see cref="System.Xml.XmlDocument">XmlDocument</see>
  /// from a <see cref="Microsoft.Data.SqlClient.SqlDataReader">SqlDataReader</see> involves
  /// creating an intermediate <see cref="System.Xml.XmlReader">XmlReader</see>.
  /// <para>This method takes care of that so the caller doesn't have to.</para>
  /// </summary>
  /// <param name="sqlDataReader"></param>
  /// <param name="columnIndex"></param>
  /// <exception cref="System.IndexOutOfRangeException">Thrown when columnIndex is out of range.</exception>
  /// <returns></returns>
  public static XmlDocument GetXmlDocument(this SqlDataReader sqlDataReader, Int32 columnIndex)
  {
    using (var xmlReader = sqlDataReader.GetSqlXml(columnIndex).CreateReader())
    {
      var xmlDocument = new XmlDocument();
      xmlDocument.Load(xmlReader);
      return xmlDocument;
    }
  }

  /// <summary>
  /// Convert a string containing xml to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
  /// <para>No validation is done on the XML.</para>
  /// </summary>
  /// <param name="xml">A <see cref="System.String">String</see> containing xml.</param>
  /// <returns>An SqlXml instance.</returns>
  public static SqlXml GetSqlXml(this String xml) => xml.GetSqlXml(new XmlReaderSettings() { ValidationType = ValidationType.None });

  /// <summary>
  /// Convert a string containing xml to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
  /// <para>The XML is validated using the xmlSchemaSet.</para>
  /// </summary>
  /// <param name="xml">A <see cref="System.String">String</see> containing xml.</param>
  /// <param name="xmlSchemaSet">An XmlSchemaSet to validate the xml against.</param>
  /// <returns>An SqlXml instance.</returns>
  public static SqlXml GetSqlXml(this String xml, XmlSchemaSet xmlSchemaSet) =>
    xml.GetSqlXml(new XmlReaderSettings() { ValidationType = ValidationType.Schema, Schemas = xmlSchemaSet });

  /// <summary>
  /// Convert a string containing xml to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
  /// </summary>
  /// <param name="xml">A <see cref="System.String">String</see> containing xml.</param>
  /// <param name="xmlReaderSettings">An instance of XmlReaderSettings that's applied to the conversion process.</param>
  /// <returns>An SqlXml instance.</returns>
  public static SqlXml GetSqlXml(this String xml, XmlReaderSettings xmlReaderSettings)
  {
    using (var stringReader = new StringReader(xml))
      using (var xmlReader = XmlReader.Create(stringReader, xmlReaderSettings))
        return new SqlXml(xmlReader);
  }

  /// <summary>
  /// Convert an XElement to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
  /// </summary>
  /// <param name="xElement">An XElement instance.</param>
  /// <param name="xmlSchemaSet">An XmlSchemaSet to validate xElement against.</param>
  /// <returns>An SqlXml instance.</returns>
  public static SqlXml GetSqlXml(this XElement xElement, XmlSchemaSet xmlSchemaSet)
  {
    var schemaInfo = xElement.GetSchemaInfo();
    if (schemaInfo != null)
    {
      var schemaElement = schemaInfo.SchemaElement;
      if (schemaElement != null)
        xElement.Validate(schemaElement, xmlSchemaSet, null /* Throw exceptions on validation error. */);
    }

    return xElement.GetSqlXml();
  }

  /// <summary>
  /// Convert an XElement to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
  /// </summary>
  /// <param name="xElement">An XElement instance.</param>
  /// <returns>An SqlXml instance.</returns>
  public static SqlXml GetSqlXml(this XElement xElement)
  {
    using (var xmlReader = xElement.CreateReader())
      return new SqlXml(xmlReader);
  }

  /// <summary>
  /// Convert an XDocument to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
  /// </summary>
  /// <param name="xmlDocument">An <see cref="System.Xml.XmlDocument">XmlDocument</see>.</param>
  /// <param name="xmlSchemaSet">An XmlSchemaSet to validate xDocument against.</param>
  /// <returns>An SqlXml instance.</returns>
  public static SqlXml GetSqlXml(this XDocument xDocument, XmlSchemaSet xmlSchemaSet)
  {
    xDocument.Validate(xmlSchemaSet, null /* Throw exceptions on validation error. */);

    return xDocument.GetSqlXml();
  }

  /// <summary>
  /// Convert an XDocument to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
  /// </summary>
  /// <param name="xmlDocument">An <see cref="System.Xml.XmlDocument">XmlDocument</see>.</param>
  /// <returns>An SqlXml instance.</returns>
  public static SqlXml GetSqlXml(this XDocument xDocument)
  {
    using (var xmlReader = xDocument.CreateReader())
      return new SqlXml(xmlReader);
  }

  /// <summary>
  /// Convert an XmlDocument to an <see cref="System.Data.SqlTypes.SqlXml">SqlXml</see> instance.
  /// </summary>
  /// <param name="xmlDocument">An <see cref="System.Xml.XmlDocument">XmlDocument</see>.</param>
  /// <param name="xmlNodeType">An <see cref="System.Xml.XmlNodeType">XmlNodeType</see>.</param>
  /// <returns></returns>
  public static SqlXml GetSqlXml(this XmlDocument xmlDocument, XmlNodeType xmlNodeType)
  {
    if ((xmlDocument.Schemas != null) && (xmlDocument.Schemas.Count > 0))
      xmlDocument.Validate(null);

    using (var xmlTextReader = new XmlTextReader(xmlDocument.InnerXml, xmlNodeType, null))
      return new SqlXml(xmlTextReader);
  }

  /// <summary>
  /// Given an SqlConnection and a database name, switch the connection to that database (if necessary) and execute action.
  /// The method will switch the connection's database back to the database it originally pointed to before action was executed.
  /// </summary>
  /// <param name="connection">A valid SqlConnection instance.</param>
  /// <param name="database">A valid database accessible by the connection.</param>
  /// <param name="action">An Action instance.</param>
  public static void ExecuteUnderDatabaseInvariant(this SqlConnection connection, String database, Action action)
  {
    /* Some operations need to change the database in order to get the data they want.

       However, it's not polite to switch a connection to a different database
       without guaranteeing that the connection will be switched back to
       its original database.

       So, this method treats the connection's current database as an invariant.
       I.e. it stores the connection's current database,
       point to the new database (if necessary), and perform the action.
       Finally, it switches the connection back to its old database (if necessary). */

    var previousDatabase = (connection.Database == database) ? null : connection.Database;
    try
    {
      if (previousDatabase != null)
        connection.ChangeDatabase(database);

      action();
    }
    finally
    {
      if (previousDatabase != null)
        connection.ChangeDatabase(previousDatabase);
    }
  }

  /// <summary>
  /// Given an SqlConnection and a stored procedure name, return an SqlParameterCollection containing
  /// all of the SqlParameters for the stored procedure.
  /// </summary>
  /// <param name="connection">A valid SqlConnection instance.</param>
  /// <param name="storedProcedureName">The name of a stored procedure accessible by the given connection.</param>
  /// <returns>An SqlParameterCollection containing all of the SqlParameters for the stored procedure.</returns>
  public static SqlParameterCollection GetSqlParameters(this SqlConnection connection, String storedProcedureName)
  {
    using (var sqlCommand = new SqlCommand(storedProcedureName, connection) { CommandType = CommandType.StoredProcedure })
    {
      SqlCommandBuilder.DeriveParameters(sqlCommand);
      return sqlCommand.Parameters;
    }
  }

  /// <summary>
  /// SqlDataRecord's SetDateTime method accepts a DateTime value, whereas this method accepts a String value.
  /// This method will do the String-to-DateTime conversion so the caller doesn't have to.
  /// </summary>
  /// <param name="dataRecord">An SqlDataRecord.</param>
  /// <param name="index">The field's index within the data record.</param>
  /// <param name="value">A string value that contains a valid DateTime value.</param>
  public static void SetDateTime(this SqlDataRecord dataRecord, Int32 index, String value) =>
    dataRecord.SetDateTime(index, Convert.ToDateTime(value));

  /// <summary>
  /// SqlDataRecord's Set* methods don't handle nulls.  This method is an abstraction that will
  /// nominally call SetDateTime(), or SetDBNull() for an empty string value.
  /// </summary>
  /// <param name="dataRecord">An SqlDataRecord.</param>
  /// <param name="index">The field's index within the data record.</param>
  /// <param name="value">A string value.  Null, empty, and whitespace values are all treated as nulls.</param>
  public static void SetNullableDateTime(this SqlDataRecord dataRecord, Int32 index, String value)
  {
    if (String.IsNullOrWhiteSpace(value))
      dataRecord.SetDBNull(index);
    else
      dataRecord.SetDateTime(index, value);
  }

  private static readonly NumberStyles _numberStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign;

  /// <summary>
  /// SqlDataRecord's SetDecimal method accepts a Decimal value, whereas this method accepts a String value.
  /// This method will do the String-to-Decimal conversion so the caller doesn't have to.
  /// </summary>
  /// <param name="dataRecord">An SqlDataRecord.</param>
  /// <param name="index">The field's index within the data record.</param>
  /// <param name="value">A string value that contains a valid Decimal value.</param>
  public static void SetDecimal(this SqlDataRecord dataRecord, Int32 index, String value) =>
    dataRecord.SetDecimal(index, Decimal.Parse(value, _numberStyle));

  /// <summary>
  /// SqlDataRecord's Set* methods don't handle nulls.  This method is an abstraction that will
  /// nominally call SetDecimal(), or SetDBNull() for an empty string value.
  /// </summary>
  /// <param name="dataRecord">An SqlDataRecord.</param>
  /// <param name="index">The field's index within the data record.</param>
  /// <param name="value">A string value.  Null, empty, and whitespace values are all treated as nulls.</param>
  public static void SetNullableDecimal(this SqlDataRecord dataRecord, Int32 index, String value)
  {
    if (String.IsNullOrWhiteSpace(value))
      dataRecord.SetDBNull(index);
    else
      dataRecord.SetDecimal(index, value);
  }

  /// <summary>
  /// SqlDataRecord's SetInt64 method accepts an Int64 value, whereas this method accepts a String value.
  /// This method will do the String-to-Int64 conversion so the caller doesn't have to.
  /// <para>This method will also correctly handle string values that have all zeros after the decimal point.</para>
  /// </summary>
  /// <param name="dataRecord">An SqlDataRecord.</param>
  /// <param name="index">The field's index within the data record.</param>
  /// <param name="value">A string value that contains a valid Int64 value.</param>
  public static void SetInt64(this SqlDataRecord dataRecord, Int32 index, String value) =>
    dataRecord.SetInt64(index, Int64.Parse(value, _numberStyle));

  /// <summary>
  /// SqlDataRecord's Set* methods don't handle nulls.  This method is an abstraction that will
  /// nominally call SetInt64(), or SetDBNull() for an empty string value.
  /// <para>This method will also correctly handle string values that have all zeros after the decimal point.</para>
  /// </summary>
  /// <param name="dataRecord">An SqlDataRecord.</param>
  /// <param name="index">The field's index within the data record.</param>
  /// <param name="value">A string value.  Null, empty, and whitespace values are all treated as nulls.</param>
  public static void SetNullableInt64(this SqlDataRecord dataRecord, Int32 index, String value)
  {
    if (String.IsNullOrWhiteSpace(value))
      dataRecord.SetDBNull(index);
    else
      dataRecord.SetInt64(index, value);
  }
}

