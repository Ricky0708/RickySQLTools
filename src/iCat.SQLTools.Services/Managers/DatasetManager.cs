﻿using iCat.SQLTools.Repositories.Interfaces;
using iCat.SQLTools.Services.Models;
using iCat.SQLTools.Shareds.Enums;
using iCat.SQLTools.Shareds.Shareds;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ZstdSharp.Unsafe;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace iCat.SQLTools.Services.Managers
{
    public class DatasetManager
    {
        private readonly IServiceProvider _provider;

        public DataSet? Dataset { get; set; }
        public DatasetFromType DatasetFromType { get; set; }

        public DatasetManager(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public string GenerateClassWithSummary(
            string classNamespace,
            string classUsing,
            string className,
            string sqlScript)
        {
            var parserResult = Microsoft.SqlServer.Management.SqlParser.Parser.Parser.Parse(sqlScript);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(parserResult.Script.Xml);
            string json = JsonConvert.SerializeXmlNode(doc);
            var obj = JObject.Parse(json.Replace("@", ""));
            return GeneratorModelFromJObject(Dataset!.Tables[Consts.strColumns]!, obj, classNamespace, classUsing, className);
        }

        public string GenerateClassWithoutSummary(string @namespace, string @using, string className, string sqlScript)
        {
            var _repository = _provider
                .GetService<IEnumerable<ISchemaRepository>>()
                .First(p => p.Category == DatasetFromType.ToString()) ?? throw new ArgumentNullException(nameof(ISchemaRepository));
            var dt = _repository.GetTableSchema(sqlScript, "schema");

            var sb = new StringBuilder();
            sb.AppendLine(@using);
            sb.AppendLine("");
            sb.AppendLine(@namespace);
            sb.AppendLine($"{{");
            sb.AppendLine($"     public class {className}");
            sb.AppendLine($"     {{");
            foreach (DataColumn col in dt.Columns)
            {
                var a = typeof(int);
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// ");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public {Convertor.GetAlias(col.DataType)}{(col.AllowDBNull ? "?" : "")} {col.ColumnName} {{ get; set; }}");
                sb.AppendLine("");
            }
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");
            var result = sb.ToString();
            return result;
        }

        #region from dataset

        private string GeneratorModelFromJObject(DataTable dtColumn, JObject jObj, string @namespace, string @using, string className)
        {
            string result = "";
            try
            {
                string defUsing = @using + "\r\n";
                string defNamespace = @namespace + "\r\n";
                string body = "";

                var selectClauses = jObj["SqlScript"]!["SqlBatch"]!["SqlSelectStatement"]!["SqlSelectSpecification"]!["SqlQuerySpecification"]!["SqlSelectClause"]!;
                var fromClauses = jObj["SqlScript"]!["SqlBatch"]!["SqlSelectStatement"]!["SqlSelectSpecification"]!["SqlQuerySpecification"]!["SqlFromClause"]!;

                var columnWithTables = GetColumnsWithTable(dtColumn, selectClauses, fromClauses);

                foreach (var col in columnWithTables)
                {
                    var summary = "";
                    var attr = "";
                    var isNullable = false;
                    var colInfo = col.Tables == null ? null : GetColumnInfo(dtColumn, col.Tables, col.SrcColumnName);
                    if (colInfo != null)
                    {
                        summary = GetSummary(colInfo) + "\r\n";
                        isNullable = (int)colInfo["IsNullable"] == 1;
                    }
                    body += summary + attr + string.Format("        public {0} {1} {{ get; set; }} {2}\r\n",
                        (Convertor.ConvertDBTypeToCSharpType(colInfo?.ItemArray[3]?.ToString()) ?? Convertor.ConvertDBTypeToCSharpType(col.ColumnType)) + (isNullable ? "?" : ""),
                        col.ColumnName,
                        (colInfo?.ItemArray[3]?.ToString() ?? col.ColumnType).ToLower() == "string" //item.DataType.Name.ToLower() == "string"
                        ? isNullable
                            ? ""
                            : " = \"\";"
                        : ""
                        );
                }

                body = body.Substring(0, body.Length - 2);
                result = defUsing + "\r\n" + defNamespace + "{\r\n    public class " + className + "\r\n    {\r\n" + body + "\r\n    }    \r\n}";
            }
            catch (Exception ex)
            {
                throw;
            }
            return result;
        }

        private List<StatementColumnWithTableModel> GetColumnsWithTable(DataTable dtColumn, JToken selectClauses, JToken fromClauses)
        {

            var tables = GetTables(fromClauses);

            var result = new List<StatementColumnWithTableModel>();
            if (selectClauses["SqlSelectStarExpression"] != null)
            {
                var expressions = selectClauses["SqlSelectStarExpression"]!;
                if (expressions.Type == JTokenType.Object)
                {
                    result.AddRange(ProcessSqlSelectStarExpression(expressions, dtColumn, tables.ToArray()));
                }
                else
                {
                    foreach (var expression in expressions)
                    {
                        result.AddRange(ProcessSqlSelectStarExpression(expression, dtColumn, tables.ToArray()));
                    }
                }
            }
            if (selectClauses["SqlSelectScalarExpression"] != null)
            {
                var expressions = selectClauses["SqlSelectScalarExpression"]!;
                if (expressions.Type == JTokenType.Object)
                {
                    result.Add(ProcessSqlSelectScalarExpression(expressions, tables.ToArray()));
                }
                else
                {
                    foreach (var expression in expressions)
                    {
                        result.Add(ProcessSqlSelectScalarExpression(expression, tables.ToArray()));
                    }
                }
            }
            return result;
        }

        private List<StatementTableModel> GetTables(JToken fromClauses)
        {
            var tables = new List<StatementTableModel>();
            var token = fromClauses;

            foreach (JProperty element in token.Children().Cast<JProperty>())
            {
                if (element.Name == "SqlQualifiedJoinTableExpression")
                {
                    tables.AddRange(GetTables(element.Value));
                }
                else if (element.Name == "SqlTableRefExpression")
                {
                    var tableRefExpressions = element.Value;
                    if (tableRefExpressions.Type == JTokenType.Object)
                    {
                        tables.Add(new StatementTableModel
                        {
                            AliasName = tableRefExpressions["Alias"]?.ToString(),
                            TableName = tableRefExpressions["ObjectIdentifier"]?.ToString()
                        });
                    }
                    else
                    {
                        tables.AddRange(tableRefExpressions.Select(p => new StatementTableModel
                        {
                            AliasName = p["Alias"]?.ToString(),
                            TableName = p["ObjectIdentifier"]?.ToString()
                        }));
                    }
                }
                else if (element.Name == "SqlDerivedTableExpression")
                {
                    var aliasName = element.Value["Alias"]?.ToString();
                    var ts = GetTables(element.Value["SqlQuerySpecification"]["SqlFromClause"]);
                    ts.ForEach(p => p.AliasName = aliasName);
                    tables.AddRange(ts);
                }
            }
            return tables;
        }

        private string GetSummary(DataRow colInfo)
        {
            string result = "";

            if (colInfo != null)
            {
                result += $"\r\n        /// <summary>";
                result += $"\r\n        /// {colInfo["ColDescription"].ToString()}";
                result += $"\r\n        /// </summary>";
            }
            return result;
        }

        private StatementColumnWithTableModel ProcessSqlSelectScalarExpression(JToken SqlSelectScalarExpression, StatementTableModel[] tables)
        {
            var result = default(StatementColumnWithTableModel);
            if (SqlSelectScalarExpression["SqlColumnRefExpression"] != null)
            {
                var columnName = SqlSelectScalarExpression["Alias"] == null ? SqlSelectScalarExpression["SqlColumnRefExpression"]["SqlObjectIdentifier"]["ObjectName"].ToString() : SqlSelectScalarExpression["Alias"].ToString();
                var srcColumnName = SqlSelectScalarExpression["SqlColumnRefExpression"]["SqlObjectIdentifier"]["ObjectName"].ToString();
                var columnTables = tables.Select(p => p.TableName).ToArray();
                result = new StatementColumnWithTableModel
                {
                    ColumnName = columnName,
                    SrcColumnName = srcColumnName,
                    Tables = columnTables
                };
            }
            else if (SqlSelectScalarExpression["SqlScalarRefExpression"] != null)
            {
                var columnName = SqlSelectScalarExpression["Alias"] == null ? SqlSelectScalarExpression["SqlScalarRefExpression"]["SqlObjectIdentifier"]["ObjectName"].ToString() : SqlSelectScalarExpression["Alias"].ToString();
                var schemaName = SqlSelectScalarExpression["SqlScalarRefExpression"]["SqlObjectIdentifier"]["SchemaName"].ToString();
                var srcColumnName = SqlSelectScalarExpression["SqlScalarRefExpression"]["SqlObjectIdentifier"]["ObjectName"].ToString();
                var columnTables = tables.Where(p => p.AliasName.ToLower() == schemaName.ToLower()).Select(p => p.TableName).ToArray();

                result = new StatementColumnWithTableModel
                {
                    ColumnName = columnName,
                    SrcColumnName = srcColumnName,
                    Tables = columnTables
                };
            }
            else if (SqlSelectScalarExpression["SqlAggregateFunctionCallExpression"] != null)
            {
                var columnName = SqlSelectScalarExpression["Alias"] == null ? SqlSelectScalarExpression["SqlAggregateFunctionCallExpression"]["SqlScalarRefExpression"]["SqlObjectIdentifier"]["ObjectName"].ToString() : SqlSelectScalarExpression["Alias"].ToString();
                var schemaName = SqlSelectScalarExpression["SqlAggregateFunctionCallExpression"]["SqlScalarRefExpression"]["SqlObjectIdentifier"]["SchemaName"].ToString();
                var srcColumnName = SqlSelectScalarExpression["SqlAggregateFunctionCallExpression"]["SqlScalarRefExpression"]["SqlObjectIdentifier"]["ObjectName"].ToString();
                var columnTables = tables.Where(p => p.AliasName.ToLower() == schemaName.ToLower()).Select(p => p.TableName).ToArray();

                result = new StatementColumnWithTableModel
                {
                    ColumnName = columnName,
                    SrcColumnName = srcColumnName,
                    Tables = columnTables
                };
            }
            else if (SqlSelectScalarExpression["SqlLiteralExpression"] != null)
            {
                var columnName = SqlSelectScalarExpression["Alias"] == null ? SqlSelectScalarExpression["SqlIdentifier"].ToString() : SqlSelectScalarExpression["Alias"].ToString();
                var columnType = SqlSelectScalarExpression["SqlLiteralExpression"]["Type"].ToString();
                result = new StatementColumnWithTableModel
                {
                    ColumnName = columnName,
                    ColumnType = columnType
                };
            }
            return result;
        }

        private StatementColumnWithTableModel[] ProcessSqlSelectStarExpression(JToken SqlSelectStarExpression, DataTable dtColumn, StatementTableModel[] tables)
        {
            var result = new List<StatementColumnWithTableModel>();
            var schemaName = SqlSelectStarExpression["Qualifier"]?.ToString();
            var columnTables = schemaName == null ? tables.Select(p => p.TableName).ToArray() : tables.Where(p => p.AliasName.ToLower() == schemaName.ToLower()).Select(p => p.TableName).ToArray();

            DataTable dtColumnsTable = dtColumn;
            var colInfos = (from p in dtColumnsTable.AsEnumerable()
                            where columnTables.Any(x => x.ToLower() == p.Field<string>("TableName").ToLower())
                            select p);
            foreach (var col in colInfos)
            {
                result.Add(new StatementColumnWithTableModel
                {
                    ColumnName = col.Field<string>("ColName")!,
                    SrcColumnName = col.Field<string>("ColName")!,
                    Tables = new[] { col.Field<string>("TableName")! }
                });
            }
            return result.ToArray();
        }

        private DataRow GetColumnInfo(DataTable dt, string[] tableNames, string colName)
        {

            DataTable dtColumnsTable = dt;
            var colInfo2 = (from p in dtColumnsTable.AsEnumerable()
                            where p.Field<string>("ColName").ToLower() == colName.ToLower() && tableNames.Any(x => x.ToLower() == p.Field<string>("TableName").ToLower())
                            select p).ToList();
            var colInfo = (from p in dtColumnsTable.AsEnumerable()
                           where p.Field<string>("ColName").ToLower() == colName.ToLower() && tableNames.Any(x => x.ToLower() == p.Field<string>("TableName").ToLower())
                           select p).Single();
            return colInfo;

        }

        #endregion
    }
}
