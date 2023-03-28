/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using Microsoft.Data.SqlClient;

using Lazy8.Core;
using System.Threading.Tasks;

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
  /// Return one or more schema tables for the given commandText. commandText may return more than one result set, and each result set
  /// has a corresponding schema table describing the result set's structure.
  /// <para>The format of the returned schema tables is described in the help entry for the
  /// <a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqldatareader.getschematable">SqlDataReader.GetSchemaTable()</a> method.</para>
  /// </summary>
  /// <param name="originalConnection">A valid SqlConnection instance.</param>
  /// <param name="commandType">A CommandType of StoredProcedure or Text.  Any other value will throw an ArgumentException.</param>
  /// <param name="commandText">A stored procedure name (for commandType == CommandType.StoredProcedure), or a valid T-SQL query (for commandType == CommandType.Text). Both are allowed to return multiple result sets.</param>
  /// <param name="parameters">Defaults to null. If provided, the parameters are cloned and the clones assigned to this method's internal SqlConnection instance.</param>
  /// <returns>An IEnumerable&lt;DataTable&gt; containing one or more schema tables.</returns>
  public static IEnumerable<DataTable> GetSchemaTables(this SqlConnection originalConnection, CommandType commandType, String commandText, SqlParameterCollection parameters = null)
  {
    originalConnection.Name(nameof(originalConnection)).NotNull();
    commandText.Name(nameof(commandText)).NotNullEmptyOrOnlyWhitespace();

    if ((commandType != CommandType.StoredProcedure) && (commandType != CommandType.Text))
      throw new ArgumentExceptionFmt(Properties.Resources.IllegalCommandType, nameof(commandType), commandType);

    using (var clonedConnection = originalConnection.GetClone())
    {
      if (clonedConnection.State != ConnectionState.Open)
        clonedConnection.Open();

      using (var command = new SqlCommand() { Connection = clonedConnection, CommandType = commandType, CommandText = commandText })
      {
        /* If parameters are provided, they have to be cloned because
           an SqlParameter cannot be a member of multiple SqlParameterCollections. */
        if (parameters != null)
          command.Parameters.AddRange(parameters.Cast<ICloneable>().Select(p => (SqlParameter) p.Clone()).ToArray());

        using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
        {
          List<DataTable> result = new();

          do
          {
            result.Add(reader.GetSchemaTable());
          } while (reader.NextResult());

          return result;
        }
      }
    }
  }

  /// <summary>
  /// Given an SqlConnection instance, return a clone of that connection.
  /// </summary>
  /// <param name="originalConnection">An SqlConnection instance.</param>
  /// <returns>An SqlConnection instance.</returns>
  public static SqlConnection GetClone(this SqlConnection originalConnection)
  {
    var clonedConnection = (SqlConnection) ((ICloneable) originalConnection).Clone();

    /* clonedConnection will always start out with a State of 'Closed', regardless of what
       originalConnection's State is. */

    if ((originalConnection.State == ConnectionState.Connecting) || (originalConnection.State == ConnectionState.Open))
      clonedConnection.Open();

    /* BUG WORKAROUND

       The current help entries for both the .NetFx and .Net Core libraries for SqlConnection.Clone() state:

         "This member is only supported by the .NET Compact Framework."

         https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection.system-icloneable-clone

         and

         https://docs.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlconnection.system-icloneable-clone

       Two things are notable - 1) the compact framework hasn't been updated since 2013, and is apparently
       abandonware, and 2) aside from that single line in the help file, the Microsoft.Data.SqlClient project
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
         throw an InvalidOperationException if clonedConnection isn't open. */
      clonedConnection.ChangeDatabase(originalConnection.Database);

    return clonedConnection;
  }

  /// <summary>
  /// Executes the given sql on the connection, and
  /// returns the result wrapped in a <see cref="System.Data.DataSet">DataSet</see>.
  /// </summary>
  /// <param name="connection"><see cref="Microsoft.Data.SqlClient.SqlConnection">SqlConnection</see> the sql is sent to.  The connection must be opened before calling this method.</param>
  /// <param name="sql"><see cref="System.String">String</see> containing sql to execute.</param>
  /// <returns></returns>
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
  /// <param name="sqlParameters">An optional array of SqlParameters.  Defaults to null.</param>
  /// <returns>A DataSet.</returns>
  public static DataSet GetDataSetFromStoredProcedure(this SqlConnection connection, String storedProcedureName, SqlParameter[] sqlParameters = null)
  {
    sqlParameters.Name(nameof(sqlParameters)).NotNull();

    /* There's no need to check sqlParameters for emptiness because parameters in a stored procedure are optional. */

    using (var command = new SqlCommand() { Connection = connection, CommandType = CommandType.StoredProcedure, CommandText = storedProcedureName })
    {
      command.Parameters.Clear();
      command.Parameters.AddRange(sqlParameters);

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
  /// Generic method to get a value from a DbDataReader instance.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="dbDataReader">A descendent of <see cref="System.Data.Common.DbDataReader">DbDataReader</see>.</param>
  /// <param name="columnName">A valid String name of one of <see cref="System.Data.Common.DbDataReader">DbDataReader</see>'s columns.</param>
  /// <returns>Either a value or default value of type T.</returns>
  public static T GetValueOrDefault<T>(this DbDataReader dbDataReader, String columnName) =>
    dbDataReader.GetValueOrDefault<T>(dbDataReader.GetOrdinal(columnName));

  /// <summary>
  /// Generic method to get a value from a DbDataReader instance.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="dbDataReader">A descendent of <see cref="System.Data.Common.DbDataReader">DbDataReader</see>.</param>
  /// <param name="columnIndex">A valid Int32 index of one of <see cref="System.Data.Common.DbDataReader">DbDataReader</see>'s columns.</param>
  /// <returns>Either a value or default value of type T.</returns>
  /// <exception cref="System.IndexOutOfRangeException">Thrown when columnIndex is out of range.</exception>
  public static T GetValueOrDefault<T>(this DbDataReader dbDataReader, Int32 columnIndex)
  {
    var type = typeof(T);
    var underlyingType = Nullable.GetUnderlyingType(type);
    var isNullableType = (underlyingType != null);
    var value = dbDataReader[columnIndex];

    return
      Convert.IsDBNull(value)
      ? default
      : (T) Convert.ChangeType(value, isNullableType ? underlyingType : type);
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
}

