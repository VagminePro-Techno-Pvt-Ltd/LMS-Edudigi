using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository
{
    public static class DatatableToModelMappingExtensions
    {
        public static List<T> MapTo<T>(this DataTable dt) where T : new()
        {
            if (dt.Rows.Count == 0)
                return new List<T>();
            return dt.MapTo<T>(null).ToList();
        }
        public static T MapToSingle<T>(this DataTable dt) where T : new()
        {
            if (dt == null || dt.Rows.Count == 0)
                return new T();
            return dt.MapTo<T>(null).First();
        }

        private static T[] MapTo<T>(this DataTable? dt, Func<PropertyInfo, bool>? propertyRestriction) where T : new()
        {
            IList<T> mappedObjects = new List<T>();
            if (dt == null || dt.Rows.Count == 0)
                return mappedObjects.ToArray();
            IEnumerable<PropertyInfo> properties =
                ((propertyRestriction != null) ?
                typeof(T).GetProperties().Where(elem => !propertyRestriction(elem)) :
                typeof(T).GetProperties());
            if (!properties.Any() || dt.Rows.Count == 0) { return mappedObjects.ToArray(); }

            foreach (DataRow dataRow in dt.Rows)
            {
                mappedObjects.Add(dataRow.MapTo<T>(properties));
            }

            return mappedObjects.ToArray();
        }

        private static T MapTo<T>(this DataRow? dataRow, IEnumerable<PropertyInfo>? properties) where T : new()
        {
            T currentObject = new();
            if (dataRow == null || properties == null)
                return currentObject;
            foreach (PropertyInfo property in properties)
            {
                String propertyName = property.Name;
                try
                {
                    var customAttr = property.GetCustomAttribute<ColumnAttribute>();
                    if (customAttr != null)
                        propertyName = customAttr.Name!;
                    if (dataRow.Table.Columns.Contains(propertyName))
                    {
                        var value = dataRow[propertyName];
                        if (!DBNull.Value.Equals(value) && property.CanWrite)
                        {
                            if (property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
                            {
                                var dValue = Convert.ToDouble(value);
                                property.SetValue(currentObject, dValue, null);
                            }
                            else
                            {
                                property.SetValue(currentObject,
                                    !DBNull.Value.Equals(value) ? value : null,
                                    null);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return currentObject;
        }
    }
}
