using Microsoft.AspNetCore.Mvc;
using TMS.ViewModels;
using TMS.Web.Extensions;
using System.Data;

namespace TMS.Web.Utilities
{
    public class SchemaUtility
    {
        public DataTable GetSchemaTable()
        {
            DataTable table = new();
            table.Columns.Add(new DataColumn()
            {
                ColumnName = "ArchitectName",
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn()
            {
                ColumnName = "DivisionName",
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn()
            {
                ColumnName = "Year",
                DataType = typeof(int)
            });
            table.Columns.Add(new DataColumn()
            {
                ColumnName = "Potential",
                DataType = typeof(decimal)
            });
            table.Columns.Add(new DataColumn()
            {
                ColumnName = "Achieved",
                DataType = typeof(decimal)
            });

            return table;
        }

        public List<ColumnConfigurationViewModel> GetSchemaList()
        {

            List<ColumnConfigurationViewModel> list = new();

            list.Add(new()
            {
                DataType = "string",
                Name = "ArchitectName",
                Nullable = "y",
                Parameter = "ArchitectName"
            });

            list.Add(new()
            {
                DataType = "string",
                Name = "DivisionName",
                Nullable = "y",
                Parameter = "DivisionName"
            });

            list.Add(new()
            {
                DataType = "int",
                Name = "Year",
                Nullable = "y",
                Parameter = "Year"
            });

            list.Add(new()
            {
                DataType = "decimal",
                Name = "Potential",
                Nullable = "y",
                Parameter = "Potential"
            });

            list.Add(new()
            {
                DataType = "decimal",
                Name = "Achieved",
                Nullable = "y",
                Parameter = "Achieved"
            });

            return list;
        }

        internal bool IsValidValue(DataRow dr, ColumnConfigurationViewModel config)
        {
            var value = Convert.ToString(dr[config.Name]);
            switch (config.DataType)
            {
                case "string":
                    if (config.IsNullable || !string.IsNullOrWhiteSpace(value))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case "bit":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (!value.Equals("y", StringComparison.InvariantCultureIgnoreCase) && !value.Equals("yes", StringComparison.InvariantCultureIgnoreCase)
                            && !value.Equals("n", StringComparison.InvariantCultureIgnoreCase) && !value.Equals("no", StringComparison.InvariantCultureIgnoreCase))
                        {
                            return true;
                        }
                    }
                    else if (config.IsNullable)
                    {
                        return true;
                    }
                    return false;
                case "decimal":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (Decimal.TryParse(value, out decimal decimalValue))
                        {
                            return true;
                        }
                        return false;
                    }
                    else if (config.IsNullable)
                    {
                        return true;
                    }
                    return false;
                case "int":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (Int32.TryParse(value, out int intValue))
                        {
                            return true;
                        }
                        return false;
                    }
                    else if (config.IsNullable)
                    {
                        return true;
                    }
                    return false;
                case "date":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (DateTime.TryParse(value, out DateTime dateValue))
                        {
                            return true;
                        }
                        return false;
                    }
                    else if (config.IsNullable)
                    {
                        return true;
                    }
                    return false;
                default:
                    return true;
            }
        }

        internal object? GetColumnValue(DataRow dr, ColumnConfigurationViewModel config)
        {
            var value = Convert.ToString(dr[config.Name]);
            switch (config.DataType)
            {
                case "string":
                    return value?.ToStringNullable();
                case "bit":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (value.Equals("y", StringComparison.InvariantCultureIgnoreCase) || value.Equals("yes", StringComparison.InvariantCultureIgnoreCase))
                        {
                            return true;
                        }
                        if (!value.Equals("n", StringComparison.InvariantCultureIgnoreCase) || !value.Equals("no", StringComparison.InvariantCultureIgnoreCase))
                        {
                            return false;
                        }
                    }

                    return null;
                case "decimal":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (Decimal.TryParse(value, out decimal decimalValue))
                        {
                            return decimalValue;
                        }
                        return null;
                    }
                    return null;
                case "int":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (Int32.TryParse(value, out int intValue))
                        {
                            return intValue;
                        }
                        return null;
                    }
                    return null;
                case "date":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (DateTime.TryParse(value, out DateTime dateValue))
                        {
                            return dateValue;
                        }
                        return null;
                    }
                    return null;
                default:
                    return null;
            }
        }

        internal bool IsFileExists(string folderLocation, string value)
        {
            string path = Path.Combine(folderLocation, value);
            if (File.Exists(path))
                return true;
            return false;
        }



    }
}
