/* Unless otherwise noted, this source code is licensed
   under the GNU Public License V3.

   See the LICENSE file in the root folder for details. */

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Lazy8.Core;

namespace Lazy8.SqlClient;

/* The use of TEXT and NTEXT types is discouraged, so they're not included in this enumeration. */

public enum StringColumnType { Char, NChar, VarChar, NVarChar }

/// <summary>
/// A collection of methods to create T-SQL DML and DDL statements from DataSet, DataTable, and DataRelation schemas and data.
/// <para>The primary use of these methods is for code generation.  Specifically, when one has a DataSet and wants to know
/// what the corresponding T-SQL DDL looks like.  The DML-generating method GetTSqlInsertStatements() isn't of much use
/// because SqlDataAdapter already has a robust set of methods to send data in a DataSet to/from whatever database it's connected to.</para>
/// </summary>
public static class GetTSqlDataTableExtensions
{
  private static String GetColumnNamesForSorting(DataTable table)
  {
    /* The AreDataTableContentsEqual() method below needs to sort the rows in
       both of its DataTable parameters before comparing them.  This method
       calculates how to most efficiently sort the DataTable, then returns a
       comma separated string of the given DataTable's column names to sort on. */

    var columns = table.Columns.Cast<DataColumn>();

    /* First check if the table has a unique column.
       If so, the first column with that attribute will be returned
       as the column to sort on. */

    var uniqueColumnName = columns.FirstOrDefault(c => c.Unique).ColumnName;
    if (uniqueColumnName != null)
      return uniqueColumnName;

    /* If there aren't any unique columns, check the table's PrimaryKey property. */

    else if ((table.PrimaryKey != null) && table.PrimaryKey.Any())
      return table.PrimaryKey.Select(pk => pk.ColumnName).Join(",");

    /* If none of the columns are unique, or there's no primary key, then
       get a list of all of the columns and use them for sorting the table. */

    else
      return columns.Select(c => c.ColumnName).Join(",");
  }

  /// <summary>
  /// Given two DataTables, return a Boolean indicating if their contents are equal.
  /// Assumes the columns in both DataTables are in the same order.
  /// </summary>
  /// <param name="table1">A DataTable.</param>
  /// <param name="table2">A DataTable.</param>
  /// <returns>A Boolean.</returns>
  public static Boolean AreDataTableContentsEqual(this DataTable table1, DataTable table2)
  {
    table1.Name(nameof(table1)).NotNull();
    table2.Name(nameof(table2)).NotNull();

    if (table1 == table2)
      throw new ArgumentException(String.Format(Properties.Resources.ReferencingTheSameDataTable, nameof(table1), nameof(table2)));
    else if ((table1.Rows.Count == 0) && (table2.Rows.Count == 0))
      return true;
    else if (table1.Rows.Count != table2.Rows.Count)
      return false;
    else if ((table1.Columns.Count == 0) && (table2.Columns.Count == 0))
      return true;
    else if (table1.Columns.Count != table2.Columns.Count)
      return false;

    /* Create views for both tables.  This allows for sorting the views without altering
       the state of the underlying tables, resulting in much better comparison performance (O(table1.Rows.Count))
       as opposed to comparing two unsorted tables (O(table1.Rows.Count * table2.Rows.Count)). */

    var view1 = new DataView(table1) { Sort = GetColumnNamesForSorting(table1) };
    var view2 = new DataView(table2) { Sort = GetColumnNamesForSorting(table2) };

    for (var n = 0; n < view1.Count; n++)
      if (!DataRowComparer.Default.Equals(view1[n].Row, view2[n].Row))
        return false;

    return true;
  }

  private static String GetSqlTypeFromClrType(Type clrType)
  {
    /* Taken from https://docs.microsoft.com/en-us/sql/relational-databases/clr-integration-database-objects-types-net-framework/mapping-clr-parameter-data */

    return clrType.Name switch
    {
      nameof(Boolean) => "BIT",
      nameof(Byte) => "TINYINT",
      nameof(Char) => "NCHAR(1)",
      nameof(DateTime) => "DATETIME",
      nameof(DateTimeOffset) => "DATETIMEOFFSET",
      nameof(Decimal) => "DECIMAL",
      nameof(Double) => "FLOAT",
      nameof(Guid) => "UNIQUEIDENTIFIER",
      nameof(Int16) => "SMALLINT",
      nameof(Int32) => "INT",
      nameof(Int64) => "BIGINT",
      nameof(Single) => "REAL",
      nameof(TimeSpan) => "TIME",
      _ => throw new ArgumentException($@"
Cannot map '{clrType.Name}' to a T-SQL type.

Use the DataColumn's ExtendedProperties with a key of 'type' to add additional
T-SQL type information.  For example:

  nameColumn.ExtendedProperties.Add(""type"", ""NVARCHAR(100)"");

Or call the AddTSqlTypeInfoForStringColumns(DataTable) method.
For String columns that do not have an extended property with a key of 'type',
that method will add the necessary type annotations based on the
longest string found in each String column.".Trim(), nameof(clrType)),
    };
  }

  private static String GetColumnDdl(DataColumn column)
  {
    var type =
      column.ExtendedProperties.ContainsKey("type")
      ? column.ExtendedProperties["type"].ToString()
      : GetSqlTypeFromClrType(column.DataType);
    var nullability =
      column.AllowDBNull
      ? "NULL"
      : "NOT NULL";

    return $"[{column.ColumnName}] {type} {nullability}";
  }

  private static String GetPrimaryKeyDirection(DataColumn column) =>
    column.ExtendedProperties.ContainsKey("primary key direction")
    ? column.ExtendedProperties["primary key direction"].ToString()
    : "ASC";

  private static String GetPrimaryKeysDdl(DataTable table) =>
    table
    .PrimaryKey
    .Select(c => $"[{c.ColumnName}] {GetPrimaryKeyDirection(c)}".Trim())
    .Join(",\n");

  private static String GetTableSchemaAndName(DataTable table)
  {
    var schema =
      table.ExtendedProperties.ContainsKey("schema")
      ? table.ExtendedProperties["schema"].ToString()
      : "dbo";

    return $"[{schema}].[{table.TableName}]";
  }

  /// <summary>
  /// Transform a DataTable's data into T-SQL INSERT statements.  Any DataTable columns that have an extended property of "excluded"
  /// will not be present in the generated INSERT statements.
  /// <para>This method isn't very useful.  One should use an SqlDataAdapter instead to read/write data from/to an
  /// SQL Server database.</para>
  /// </summary>
  /// <param name="table">A DataTable.</param>
  /// <returns>A String consisting of T-SQL INSERT statements.</returns>
  public static String GetTSqlInsertStatements(this DataTable table)
  {
    var columns =
      table
      .Columns
      .Cast<DataColumn>()
      .Where(c => !c.ExtendedProperties.ContainsKey("excluded"));
    var columnNames =
      columns
      .Select(c => $"[{c.ColumnName}]")
      .Join(",\n")
      .Indent(4)
      .Trim();
    var rows =
      table
      .AsEnumerable()
      .Select(r => GetRowValuesAsString(columns, r))
      .Join(",\n")
      .Indent(4)
      .Trim();

    return $@"
INSERT INTO {GetTableSchemaAndName(table)}
  (
    {columnNames}
  )
  VALUES
    {rows};
GO
".TrimStart();
  }

  private static String GetRowValuesAsString(IEnumerable<DataColumn> columns, DataRow row)
  {
    List<String> result = [];

    foreach (var column in columns)
    {
      if (row.IsNull(column))
      {
        result.Add("NULL");
      }
      else if (column.DataType == typeof(String))
      {
        /* Regardless of the underlying T-SQL type (CHAR, VARCHAR, NCHAR, etc.)
           all string literals need to be surrounded with single quotes, and any single quotes w/i the string
           need to be duplicated. */

        result.Add($"'{row[column].ToString().Replace("'", "''")}'");
      }
      else if (column.DataType == typeof(DateTime))
      {
        /* SQL Server date and time literals are non-trivial. See https://docs.microsoft.com/en-us/sql/t-sql/data-types/datetime-transact-sql
           for details. 

           This algorithm first checks for type annotations in column's ExtendedProperties. */

        var dt = Convert.ToDateTime(row[column]);

        if (column.ExtendedProperties.ContainsKey("type"))
        {
          var type = column.ExtendedProperties["type"].ToString();

          switch (type)
          {
            case "date":
              result.Add($"'{dt:yyyy-MM-dd}'");
              break;
            case "datetime":
              result.Add($"'{dt:yyyy-MM-ddThh:mm:ss.fff}'");
              break;
            case "datetime2":
              result.Add($"'{dt:yyyy-MM-ddThh:mm:ss.fffffff}'");
              break;
            case "datetimeoffset":
              result.Add($"'{DateTime.SpecifyKind(dt, DateTimeKind.Local):O}'");
              break;
            case "smalldatetime":
              result.Add($"'{dt:yyyy-MM-dd hh:mm:ss}'");
              break;
            case "time":
              result.Add($"'{dt:hh:mm:ss.fffffff}'");
              break;
            default:
              throw new Exception($"Unknown type '{type}'.  When used in a DataColumn's ExtendedProperties, T-SQL types are case-sensitive.  Allowed types are 'date', 'datetime', 'datetime2', 'datetimeoffset', 'smalldatetime', and 'time'.");
          }
        }
        else
        {
          /* If there aren't any ExtendedProperties type annotations, just assume a T-SQL type of DATETIME. */

          result.Add($"'{dt:yyyy-MM-ddThh:mm:ss.fff}'");
        }
      }
      else
      {
        result.Add(row[column].ToString());
      }
    }

    return $"({result.Join(", ")})";
  }

  /// <summary>
  /// Given a DataTable, remove all DataColumn extended properties with a key of 'excluded'.
  /// </summary>
  /// <param name="table">A DataTable.</param>
  /// <returns>The modified DataTable.</returns>
  public static DataTable ClearAllExcludedColumnFlags(this DataTable table)
  {
    var columnNames =
      table
      .Columns
      .Cast<DataColumn>()
      .Select(c => c.ColumnName)
      .ToArray();

    table.ClearExcludedColumnFlag(columnNames);

    return table;
  }

  /// <summary>
  /// Given a DataTable and an array of column names, remove all DataColumn extended properties with a key of 'excluded' from
  /// those columns.
  /// </summary>
  /// <param name="table">A DataTable.</param>
  /// <param name="columnNames">An array of strings containing the names of the columns to modify.</param>
  /// <returns>The modified DataTable.</returns>
  public static DataTable ClearExcludedColumnFlag(this DataTable table, params String[] columnNames)
  {
    var columnsToClear =
      table
      .Columns
      .Cast<DataColumn>()
      .Where(c => columnNames.ContainsCI(c.ColumnName));

    foreach (var column in columnsToClear)
      if (column.ExtendedProperties.ContainsKey("excluded"))
        column.ExtendedProperties.Remove("excluded");

    return table;
  }

  /// <summary>
  /// Given a DataTable and an array of column names, set all DataColumn extended properties to a key of 'excluded' in
  /// those columns.
  /// </summary>
  /// <param name="table">A DataTable.</param>
  /// <param name="columnNames">An array of strings containing the names of the columns to modify.</param>
  /// <returns>The modified DataTable.</returns>
  public static DataTable SetExcludedColumnFlag(this DataTable table, params String[] columnNames)
  {
    var columnsToExclude =
      table
      .Columns
      .Cast<DataColumn>()
      .Where(c => columnNames.ContainsCI(c.ColumnName));

    foreach (var column in columnsToExclude)
      if (!column.ExtendedProperties.ContainsKey("excluded"))
        column.ExtendedProperties.Add("excluded", "");

    return table;
  }

  /// <summary>
  /// Note that T-SQL types such as CHAR and NVARCHAR have a length requirement, whereas a DataTable column with type String does not.
  /// <para>In order to generate T-SQL DDL from a DataSet or DataTable, all String columns in a DataTable must have an extended property of 'type'.
  /// This extended property should contain a T-SQL character type with an accompanying length, like 'NCHAR(10)'.</para>
  /// <para>To automate this process, this method will scan all of the data in the given DataTable's String columns and add the appropriate
  /// 'type' extended property.</para>
  /// </summary>
  /// <param name="table">A DataTable.</param>
  /// <param name="stringColumnType">An enumeration specifying what kind of T-SQL type to use in the extended 'type' property.</param>
  /// <returns>The modified DataTable.</returns>
  public static DataTable AddTSqlTypeInfoForStringColumns(this DataTable table, StringColumnType stringColumnType = StringColumnType.NVarChar)
  {
    /* To explain the magic numbers 8000 and 4000, see the T-SQL CHAR* help entries
       for descriptions of their respective column width limits:

         https://docs.microsoft.com/en-us/sql/t-sql/data-types/char-and-varchar-transact-sql
         https://docs.microsoft.com/en-us/sql/t-sql/data-types/nchar-and-nvarchar-transact-sql */

    Int32 getMaximumAllowableWidth() =>
      stringColumnType switch
      {
        StringColumnType.Char or StringColumnType.VarChar => 8000,
        StringColumnType.NChar or StringColumnType.NVarChar => 4000,
        _ => throw new Exception($"Unknown value given for StringColumnType enum ('{stringColumnType}')."),
      };

    var stringColumns =
      table
      .Columns
      .Cast<DataColumn>()
      .Where(c => c.DataType == typeof(String));

    foreach (var column in stringColumns)
    {
      if (!column.ExtendedProperties.ContainsKey("type"))
      {
        var lengthOfWidestColumn =
          table
         .AsEnumerable()
         .Select(row => row[column.ColumnName].ToString().Length)
         .Max();
        var widthSpecifier = (lengthOfWidestColumn <= getMaximumAllowableWidth()) ? lengthOfWidestColumn.ToString() : "MAX";

        column.ExtendedProperties.Add("type", $"{stringColumnType}({widthSpecifier})");
      }
    }

    return table;
  }

  /// <summary>
  /// Given a DataTable, return a string consisting of T-SQL DDL commands that will create that DataTable's column schema
  /// and primary key contraint on SQL Server.
  /// </summary>
  /// <remarks>
  /// Generation of T-SQL is guided by metadata found in the DataTable and its DataColumns.
  /// ExtendedProperties properties may be specified in DataTables and DataColumns to generate more accurate T-SQL.
  /// <para>
  /// The recognized metadata keys (case sensitive) for DataTables are:
  /// </para>
  /// <list type="bullet">
  ///   <item>
  ///     <description>schema</description>
  ///   </item>
  /// </list>
  /// <example>
  ///  Example:
  /// <code>
  ///  DataTable dt = new("item");
  ///  dt.ExtendedProperties.Add("schema", "inventory");
  /// </code>
  /// </example>
  /// <para>
  /// The recognized metadata keys (case sensitive) for DataColumns are:
  /// </para>
  /// <list type="table">
  ///   <listheader>
  ///     <term>Key</term>
  ///     <description>Valid Values</description>
  ///   </listheader>
  ///   <item>
  ///     <term>excluded</term>
  ///     <description>Applicable for both DataTable and DataColumn.  No value necessary.
  ///     The presence of this key will omit the table or column from code generation.</description>
  ///   </item>
  ///   <item>
  ///     <term>primary key direction</term>
  ///     <description>Valid values are "asc" and "desc" (case sensitive).</description>
  ///   </item>
  ///   <item>
  ///     <term>type</term>
  ///     <description>Valid values are any T-SQL data type declaration (e.g. NVARCHAR(50)).  The value is not case sensitive.</description>
  ///   </item>
  /// </list>
  /// <example>
  ///  Example 1:
  /// <code>
  ///  DataColumn descriptionColumn = new DataColumn("description", typeof(String)) { AllowDBNull = false };
  ///  descriptionColumn.ExtendedProperties.Add("primary key direction", "desc");
  ///  descriptionColumn.ExtendedProperties.Add("type", "nvarchar(100)");
  /// </code>
  /// </example>
  /// <example>
  ///  Example 2:
  /// <code>
  ///  DataColumn descriptionColumn = new DataColumn("quantity", typeof(Int32)) { AllowDBNull = false };
  ///  // "excluded" is a flag indicating this column will be excluded from the code generation process. The value doesn't matter.
  ///  descriptionColumn.ExtendedProperties.Add("excluded", "");
  /// </code>
  /// </example>
  /// </remarks>
  /// <param name="table">A DataTable.
  /// The DataTable's DataColumns may contain extended properties as described in the 'Remarks' section.
  /// A DataColumn's Expression property will be copied into the DDL without modification.</param>
  /// <returns>A String as described in the summary.</returns>
  public static String GetTSqlDdl(this DataTable table)
  {
    var columns = table.Columns.Cast<DataColumn>().Where(c => !c.ExtendedProperties.ContainsKey("excluded"));
    var primaryKeyConstraint =
      table.PrimaryKey.Any()
      ? $@"
CONSTRAINT [PK_{table.TableName}] PRIMARY KEY CLUSTERED 
(
  {GetPrimaryKeysDdl(table).Indent(2).Trim()}
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
"
      : "";

    return $@"
CREATE TABLE {GetTableSchemaAndName(table)}
(
  {columns.Select(GetColumnDdl).Join(",\n").Indent(2).Trim()}

  {primaryKeyConstraint.Indent(2).Trim()}
) ON [PRIMARY];
GO
";
  }

  private static String GetForeignKeysDdl(DataSet dataSet)
  {
    return
      dataSet
      .Relations
      .Cast<DataRelation>()
      .Select(r => $@"
ALTER TABLE {GetTableSchemaAndName(r.ChildTable)} WITH CHECK ADD CONSTRAINT [{r.RelationName}] FOREIGN KEY ([{r.ChildColumns.Select(c => c.ColumnName).Join(", ")}]) REFERENCES {GetTableSchemaAndName(r.ParentTable)} ([{r.ParentColumns.Select(c => c.ColumnName).Join(", ")}]);
GO

ALTER TABLE {GetTableSchemaAndName(r.ChildTable)} CHECK CONSTRAINT [{r.RelationName}];
GO")
      .Join("\n");
  }

  /// <summary>
  /// Given a DataSet, return a string consisting of T-SQL DDL commands that will create that DataSet's schema on SQL Server.
  /// </summary>
  /// <remarks>
  /// Generation of T-SQL is guided by metadata found in the DataTables, DataColumns, and DataRelations in the dataSet.
  /// ExtendedProperties properties may be specified in DataTables and DataColumns to generate more accurate T-SQL.
  /// <para>
  /// The recognized metadata keys (case sensitive) for DataTables are:
  /// </para>
  /// <list type="bullet">
  ///   <item>
  ///     <description>schema</description>
  ///   </item>
  /// </list>
  /// <example>
  ///  Example:
  /// <code>
  ///  DataTable dt = new("item");
  ///  dt.ExtendedProperties.Add("schema", "inventory");
  /// </code>
  /// </example>
  /// <para>
  /// The recognized metadata keys (case sensitive) for DataColumns are:
  /// </para>
  /// <list type="table">
  ///   <listheader>
  ///     <term>Key</term>
  ///     <description>Valid Values</description>
  ///   </listheader>
  ///   <item>
  ///     <term>excluded</term>
  ///     <description>Applicable for both DataTable and DataColumn.  No value necessary.
  ///     The presence of this key will omit the table or column from code generation.</description>
  ///   </item>
  ///   <item>
  ///     <term>primary key direction</term>
  ///     <description>Valid values are "asc" and "desc" (case sensitive).</description>
  ///   </item>
  ///   <item>
  ///     <term>type</term>
  ///     <description>Valid values are any T-SQL data type declaration (e.g. NVARCHAR(50)).  The value is not case sensitive.</description>
  ///   </item>
  /// </list>
  /// <example>
  ///  Example 1:
  /// <code>
  ///  DataColumn descriptionColumn = new DataColumn("description", typeof(String)) { AllowDBNull = false };
  ///  descriptionColumn.ExtendedProperties.Add("primary key direction", "desc");
  ///  descriptionColumn.ExtendedProperties.Add("type", "nvarchar(100)");
  /// </code>
  /// </example>
  /// <example>
  ///  Example 2:
  /// <code>
  ///  DataColumn descriptionColumn = new DataColumn("quantity", typeof(Int32)) { AllowDBNull = false };
  ///  // "excluded" is a flag indicating this column will be excluded from the code generation process. The value doesn't matter.
  ///  descriptionColumn.ExtendedProperties.Add("excluded", "");
  /// </code>
  /// </example>
  /// </remarks>
  /// <param name="dataSet">A DataSet with one or more DataTables, and zero or more DataRelations.
  /// The DataTable's DataColumns may contain extended properties as described in the 'Remarks' section.
  /// A DataColumn's Expression property will be copied into the DDL without modification. Foreign key info will be derived from
  /// any DataRelations that are present.</param>
  /// <returns>A String as described in the summary.</returns>
  public static String GetTSqlDdl(this DataSet dataSet)
  {
    var schemaSqlTemplate = @"
IF NOT EXISTS(SELECT * FROM sys.schemas WHERE [name] = '{0}')
BEGIN
  CREATE SCHEMA [{0}];
END;
GO
";
    var tables = dataSet.Tables.Cast<DataTable>();
    var createSchemasDdl =
      tables
      .Where(t => t.ExtendedProperties.Contains("schema"))
      .Select(t => t.ExtendedProperties["schema"].ToString())
      .Distinct()
      .Select(schemaName => String.Format(schemaSqlTemplate, schemaName))
      .Join("\n");
    var createTablesDdl = tables.Select(t => t.GetTSqlDdl()).Join("\n");

    return $"{createSchemasDdl}\n{createTablesDdl}\n{GetForeignKeysDdl(dataSet)}";
  }
}

