using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository
{
    public static class ExpressionUtility
    {
        public static Expression<Func<TTarget, bool>> Convert<TSource, TTarget>(this Expression<Func<TSource, bool>> expression, bool isModel = false)
            where TTarget : class
            where TSource : class
        {
            var serializer = new Serialize.Linq.Serializers.ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer());
            serializer.AddKnownTypes(new[] { typeof(int[]) });
            string value = serializer.SerializeText(expression);
            value = value.Replace(typeof(TSource).ToString(), typeof(TTarget).ToString());            
            if (isModel)
                value = value.Replace(typeof(ViewModels.BaseViewModel).ToString(), typeof(Models.BaseModel).ToString());
            else
                value = value.Replace(typeof(ViewModels.BaseMasterViewModel).ToString(), typeof(Models.BaseMasterModel).ToString());
            value = value.Replace("MasterViewModel", "Master").Replace(".ViewModels", ".Models").Replace("ViewModel", "Model");
            return (Expression<Func<TTarget, bool>>)serializer.DeserializeText(value);

        }
        public static Expression<Func<TTarget>> Convert<TSource, TTarget>(this Expression<Func<TSource>> expression, bool isModel = false)
            where TTarget : class
            where TSource : class
        {
            var serializer = new Serialize.Linq.Serializers.ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer());
            serializer.AddKnownTypes(new[] { typeof(int[]) });
            string value = serializer.SerializeText(expression);
            value = value.Replace(typeof(TSource).ToString(), typeof(TTarget).ToString());
            value = value.Replace("ViewModel", "Model");
            if (isModel)
                value = value.Replace(typeof(ViewModels.BaseViewModel).ToString(), typeof(Models.BaseModel).ToString());
            else
                value = value.Replace(typeof(ViewModels.BaseMasterViewModel).ToString(), typeof(Models.BaseMasterModel).ToString());
            return (Expression<Func<TTarget>>)serializer.DeserializeText(value);
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            List<string> columnsToBeRemoved = new List<string>();
            //Get all the properties  
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                var customAttr = prop.GetCustomAttribute<NotMappedAttribute>();
                if (customAttr != null)
                    columnsToBeRemoved.Add(prop.Name);
                //Defining type of data column gives proper data table   
                var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType);
                if (prop.PropertyType.IsEnum)
                    type = typeof(Int32);
                //Setting column names as Property names  
                dataTable.Columns.Add(prop.Name, type!);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows  
                    var val = Props[i].GetValue(item, null);
                    if (val != null)
                        values[i] = val;
                }
                dataTable.Rows.Add(values);
            }
            if (columnsToBeRemoved.Count > 0)
            {
                foreach (var column in columnsToBeRemoved)
                {
                    dataTable.Columns.Remove(column);
                }
            }
            return dataTable;
        }
    }
}
